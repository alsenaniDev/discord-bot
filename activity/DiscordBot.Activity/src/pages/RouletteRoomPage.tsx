import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { RouletteWheel } from '../components/roulette/RouletteWheel';
import { getRouletteCapabilities, getRouletteRoom, getStore, joinRouletteRoom, leaveRouletteRoom, reconnectRouletteRoom, resolveRoulettePendingAction, spinRoulette, startRouletteRoom, useRoulettePowerUp } from '../lib/api';
import { joinRouletteGameSession, onActivitiesReconnected, onRouletteEvent } from '../lib/activitiesSignalR';
import { useActivity } from '../context/ActivityProvider';
import type { PowerUpStoreItem, RoulettePlayer, RouletteRoom, RouletteRuntimeCapabilities } from '../types';
import './RoulettePage.css';

const statusText: Record<string, string> = { Waiting: 'بانتظار اللاعبين', InProgress: 'اللعبة جارية', Completed: 'اكتملت اللعبة', Cancelled: 'أُلغيت الغرفة', Expired: 'انتهت مدة الانضمام' };
const displayName = (player?: RoulettePlayer | null, fallback = 'لاعب') => player?.displayName || player?.username || fallback;
const initials = (value: string) => value.trim().slice(0, 2) || '؟';
const powerClass = (key: string) => key === 'shield' ? 'shield' : key === 'reverse' ? 'reverse' : 'respin';
const powerLabel = (item: PowerUpStoreItem) => item.key === 'shield' ? 'استخدام الدرع' : item.key === 'reverse' ? 'عكس الهجمة' : item.key === 'respin' ? 'إعادة اللف' : item.name;
const soundHooks = { onSpinStart() {}, onSpinEnd() {}, onPowerUpUsed() {}, onVictory() {} };

function PlayerToken({ player, current, pending }: { player: RoulettePlayer; current: boolean; pending: boolean }) {
  const name = displayName(player);
  return <div className={`player-token ${player.isAlive ? '' : 'eliminated'} ${current ? 'current pulse-turn' : ''} ${pending ? 'pending' : ''}`}>
    <span className="game-avatar">{player.avatarUrl ? <img src={player.avatarUrl} alt="" /> : player.isHost ? '👑' : initials(name)}</span>
    <div><strong>{name}</strong><small>{player.isAlive ? current ? 'الدور الآن' : pending ? 'مستهدف' : 'داخل التحدي' : 'تم إقصاؤه'}</small></div>
  </div>;
}

export function RouletteRoomPage() {
  const { roomId = '' } = useParams(); const { identity } = useActivity(); const navigate = useNavigate();
  const [room, setRoom] = useState<RouletteRoom | null>(null); const [inventory, setInventory] = useState<PowerUpStoreItem[]>([]); const [balance, setBalance] = useState(0); const [busy, setBusy] = useState(false); const [error, setError] = useState(''); const [event, setEvent] = useState(''); const [now, setNow] = useState(Date.now()); const [spinning, setSpinning] = useState(false); const [flash, setFlash] = useState(''); const [spinOrder, setSpinOrder] = useState<RoulettePlayer[] | null>(null); const [spinSelectedIndex, setSpinSelectedIndex] = useState<number | null>(null);
  const [capabilities, setCapabilities] = useState<RouletteRuntimeCapabilities>({ runtimeVersion: 'legacy', supportsWalletBets: true, supportsPowerUps: true, supportsReconnect: false });
  const resolvingRef = useRef(false);
  const spinningRef = useRef(false);
  const queuedRoomRef = useRef<RouletteRoom | null>(null);
  const load = async () => { if (!identity || !roomId) return; try { const value = await getRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId); if (spinningRef.current) queuedRoomRef.current = value; else setRoom(value); try { const store = await getStore(identity.accessToken, identity.guildId); setInventory(store.items); setBalance(store.balance); } catch { /* Keep room usable if store refresh fails. */ } } catch (e) { setError(e instanceof Error ? e.message : 'تعذر تحميل الغرفة.'); } };
  useEffect(() => { void load(); }, [identity, roomId]);
  useEffect(() => { if (!identity) return; getRouletteCapabilities(identity.accessToken, identity.guildId).then(setCapabilities).catch(() => undefined); }, [identity]);
  useEffect(() => {
    if (!identity || !roomId || !capabilities.supportsReconnect) return;
    let active = true;
    const reconnect = async () => {
      if (!active) return;
      try {
        const snapshot = await reconnectRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId);
        if (!active) return;
        setRoom(snapshot);
        if (!['Completed', 'Cancelled', 'Expired'].includes(snapshot.status)) await joinRouletteGameSession(snapshot.gameSessionId);
        setEvent(snapshot.status === 'Completed' ? 'تمت استعادة نتيجة اللعبة.' : 'تمت استعادة الاتصال بالغرفة.');
      } catch (e) {
        if (active) setError(e instanceof Error ? e.message : 'تعذر استعادة الاتصال بالغرفة.');
      }
    };
    const offReconnect = onActivitiesReconnected(reconnect);
    const offEvent = onRouletteEvent(roomId, evt => {
      const snapshot = (evt as { payload?: RouletteRoom }).payload;
      if (snapshot?.id === roomId && !spinningRef.current) setRoom(snapshot);
    });
    reconnect();
    return () => { active = false; offReconnect(); offEvent(); };
  }, [identity, roomId, capabilities.supportsReconnect]);
  useEffect(() => { const id = window.setInterval(() => setNow(Date.now()), 1000); return () => window.clearInterval(id); }, []);
  useEffect(() => { if (!room || !['Waiting', 'InProgress'].includes(room.status)) return; const id = window.setInterval(() => void load(), 2500); return () => window.clearInterval(id); }, [room?.status, identity, roomId]);
  const me = useMemo(() => room?.players.find(x => x.userDiscordId === identity?.userId), [room, identity]); const isHost = room?.hostUserDiscordId === identity?.userId;
  const isMyTurn = room?.currentTurnUserDiscordId === identity?.userId; const isPendingTarget = room?.pendingTargetUserDiscordId === identity?.userId && room?.pendingActionStatus === 'WaitingForPowerUp';
  const joinSeconds = room ? Math.max(0, Math.ceil((new Date(room.expiresAt).getTime() - now) / 1000)) : 0;
  const pendingSeconds = room?.pendingActionExpiresAt ? Math.max(0, Math.ceil((new Date(room.pendingActionExpiresAt).getTime() - now) / 1000)) : 0;
  useEffect(() => {
    if (!identity || !room || spinningRef.current || resolvingRef.current) return;
    const shouldResolve = room.pendingActionStatus === 'AutoResolved' || (room.pendingActionStatus === 'WaitingForPowerUp' && pendingSeconds <= 0);
    if (!shouldResolve) return;
    resolvingRef.current = true;
    resolveRoulettePendingAction(identity.accessToken, room.id, identity.guildId, identity.channelId)
      .then(value => { setRoom(value); if (room.pendingActionStatus === 'AutoResolved') setEvent(`${room.pendingTargetUsername ?? 'اللاعب'} لا يملك خصائص متاحة وتم تنفيذ النتيجة.`); })
      .catch(() => undefined)
      .finally(() => { resolvingRef.current = false; });
  }, [pendingSeconds, room?.pendingActionStatus, room?.id, identity]);
  useEffect(() => { if (room?.status === 'Completed') soundHooks.onVictory(); }, [room?.status]);
  const act = async (fn: () => Promise<RouletteRoom>, success = '') => { if (busy) return; setBusy(true); setError(''); try { const value = await fn(); setRoom(value); setEvent(success); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر إكمال الطلب.'); } finally { setBusy(false); } };
  const leave = async () => { if (busy || !identity) return; setBusy(true); setError(''); try { await leaveRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId); navigate('/games/roulette', { replace: true }); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر مغادرة الغرفة.'); setBusy(false); } };
  const spin = async () => {
    if (!identity || busy) return;
    soundHooks.onSpinStart(); setBusy(true); setSpinning(true); spinningRef.current = true; queuedRoomRef.current = null; setSpinSelectedIndex(null); setError('');
    try {
      const value = await spinRoulette(identity.accessToken, roomId, identity.guildId, identity.channelId);
      const orderedAlive = value.alivePlayers?.length ? value.alivePlayers : value.room.alivePlayers?.length ? value.room.alivePlayers : value.room.players.filter(x => x.isAlive);
      setSpinOrder(orderedAlive); setSpinSelectedIndex(value.selectedIndex);
      window.setTimeout(() => {
        if (!value.targetHasUsablePowerUps) resolvingRef.current = true;
        setRoom(value.room);
        const targetName = displayName(value.targetPlayer);
        setEvent(value.targetPlayer ? `وقفت العجلة على ${targetName}` : 'تم تدوير العجلة.');
        setSpinning(false); spinningRef.current = false; setBusy(false); soundHooks.onSpinEnd();
        if (!value.targetHasUsablePowerUps) {
          window.setTimeout(() => {
            if (!identity) return;
            resolveRoulettePendingAction(identity.accessToken, roomId, identity.guildId, identity.channelId)
              .then(updated => { setRoom(updated); setEvent(`${targetName} لا يملك خصائص متاحة وتم تنفيذ النتيجة.`); })
              .catch(() => undefined)
              .finally(() => { resolvingRef.current = false; void load(); });
          }, 650);
        } else {
          queuedRoomRef.current = null; void load();
        }
      }, 3000);
    } catch (e) {
      setSpinning(false); spinningRef.current = false; setBusy(false); setSpinOrder(null); setSpinSelectedIndex(null); setError(e instanceof Error ? e.message : 'تعذر تدوير الروليت.');
    }
  };
  const usePower = async (item: PowerUpStoreItem) => { if (!identity || busy) return; if (!capabilities.supportsPowerUps) { setError('الخصائص غير متاحة في تجربة الروليت الجديدة حاليًا.'); return; } setBusy(true); setError(''); try { const value = await useRoulettePowerUp(identity.accessToken, roomId, identity.guildId, identity.channelId, item.key); setRoom(value); setEvent(`${item.icon} تم استخدام ${item.name}`); setFlash(item.key); soundHooks.onPowerUpUsed(); window.setTimeout(() => setFlash(''), 850); void load(); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر استخدام الخاصية.'); } finally { setBusy(false); } };
  if (!room || !identity) return <main className="center-state game-page"><div className="loader"/><p>{error || 'جاري تحميل غرفة الروليت...'}</p></main>;
  const alive = room.alivePlayers?.length ? room.alivePlayers : room.players.filter(x => x.isAlive); const eliminated = room.eliminatedPlayers?.length ? room.eliminatedPlayers : room.players.filter(x => !x.isAlive); const usable = capabilities.supportsPowerUps ? inventory.filter(x => x.isEnabledForGuild && x.ownedQuantity > 0) : [];
  const storePath = `/store?returnTo=${encodeURIComponent(`/games/roulette/room/${roomId}`)}`;
  const selectedUserId = room.pendingTargetUserDiscordId ?? room.lastSpinResult?.targetUserDiscordId;
  const wheelPlayers = spinning && spinOrder?.length ? spinOrder : alive;
  const selectedWheelIndex = spinning ? spinSelectedIndex : room.lastSpinResult?.selectedIndex ?? null;
  const latestPowerUpAction = room.actions.find(action => action.powerUpKey);
  const hasPending = room.pendingActionStatus === 'WaitingForPowerUp' || room.pendingActionStatus === 'AutoResolved';
  return <main className={`roulette-page game-page flash-${flash}`}><div className="game-shell narrow">
    <header className="roulette-topbar">
      <div className="roulette-title"><button className="text-button" onClick={() => navigate('/games/roulette')}>←</button><span className="roulette-logo">🎡</span><div><h2>الروليت</h2><p>{statusText[room.status]}</p></div></div>
      <div className="hero-actions"><span className="game-coin">💰 رصيدك: {balance}</span><span className="game-status">الجولة {room.currentRound || 0}</span></div>
    </header>
    {error && <p className="error-text">{error}</p>}
    {room.status === 'Waiting' && <section className="waiting-room game-glass">
      <div className="waiting-hero"><span className="roulette-logo">🎡</span><h1>غرفة الروليت</h1><p>اجمع اللاعبين وابدأ التحدي<span className="waiting-dots" /></p></div>
      <div className="room-stats-grid"><div className="room-stat"><span>المضيف</span><strong>👑 {room.hostUsername}</strong></div><div className="room-stat"><span>اللاعبون</span><strong>{room.players.length} / {room.maxPlayers}</strong></div><div className="room-stat"><span>ينتهي الانضمام خلال</span><strong>{joinSeconds} ثانية</strong></div></div>
      <div className="player-panel game-glass"><div className="section-head"><div><h2>اللاعبون</h2><p className="game-help">الحد الأدنى للبدء: {room.minPlayers}</p></div></div><div className="players-grid">{room.players.map(player => <PlayerToken key={player.userDiscordId} player={player} current={false} pending={false} />)}</div></div>
      {event && <div className="toast-game">{event}</div>}
      <div className="roulette-actions">{!me && <button className="game-button gold" disabled={busy} onClick={() => void act(() => joinRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId), 'تم الانضمام للتحدي')}>ادخل التحدي</button>}{isHost && <button className="game-button primary" disabled={busy || !room.canStart} onClick={() => void act(() => startRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId), 'بدأت اللعبة!')}>{room.canStart ? 'بدء اللعبة' : 'بانتظار لاعبين إضافيين'}</button>}{me && <button className="game-button secondary" disabled={busy} onClick={() => void leave()}>مغادرة الغرفة</button>}<button className="game-button secondary" onClick={() => navigate(storePath)}>🛒 المتجر</button></div>
    </section>}
    {room.status === 'InProgress' && <div className="arena-grid">
      <section className={`arena-main game-glass ${flash ? 'pulse-turn' : ''}`}>
        <div className="turn-banner pulse-turn">{room.currentTurnPlayer?.avatarUrl && <span className="game-avatar"><img src={room.currentTurnPlayer.avatarUrl} alt="" /></span>}<div><span>الدور الآن</span><strong>{displayName(room.currentTurnPlayer, room.currentTurnUsername ?? 'غير محدد')}</strong><small>{isMyTurn ? 'دورك! لف العجلة الآن' : `بانتظار ${room.currentTurnUsername ?? 'اللاعب'}`}</small></div></div>
        {hasPending && <div className="pending-panel game-glass"><h2>العجلة وقفت على {displayName(room.pendingTargetPlayer, room.pendingTargetUsername ?? 'اللاعب')}</h2><p>{room.pendingActionStatus === 'AutoResolved' ? 'لا توجد خصائص متاحة لهذا اللاعب، سيتم تنفيذ النتيجة مباشرة.' : isPendingTarget ? <>لديك <span className="pending-count">{pendingSeconds}</span> ثانية لاستخدام خاصية!</> : `بانتظار قرار ${room.pendingTargetUsername}...`}</p>{isPendingTarget && <div className="powerup-actions">{!capabilities.supportsPowerUps ? <p className="game-help">الخصائص غير متاحة في تجربة الروليت الجديدة حاليًا.</p> : usable.length === 0 ? <><p className="game-help">لا تملك خصائص متاحة لهذا الدور.</p><button className="game-button secondary" onClick={() => navigate(storePath)}>فتح المتجر</button></> : usable.map(item => <button className={`game-button ${powerClass(item.key)}`} disabled={busy} onClick={() => void usePower(item)} key={item.key}>{item.icon} {powerLabel(item)} · {item.ownedQuantity}</button>)}</div>}</div>}
        {latestPowerUpAction && <div className={`powerup-banner game-glass ${latestPowerUpAction.powerUpKey ?? ''}`}><span>{latestPowerUpAction.powerUpIcon ?? '✨'}</span><div><strong>{latestPowerUpAction.powerUpName ?? 'خاصية'}</strong><p>{latestPowerUpAction.message}</p></div></div>}
        <div className="arena-center"><RouletteWheel players={wheelPlayers} spinning={spinning} selectedUserDiscordId={selectedUserId} selectedIndex={selectedWheelIndex} /><button className="game-button danger spin-cta" disabled={busy || !isMyTurn || hasPending} onClick={() => void spin()}>{spinning ? 'العجلة تدور...' : isMyTurn ? 'تدوير العجلة' : 'بانتظار دورك'}</button>{event && <div className="wheel-result">{event}</div>}</div>
      </section>
      <aside className="arena-side">
        <section className="player-panel game-glass"><div className="section-head"><div><h2>اللاعبون</h2><p className="game-help">الحيّون {alive.length}</p></div></div><div className="players-grid">{alive.map(player => <PlayerToken key={player.userDiscordId} player={player} current={player.userDiscordId === room.currentTurnUserDiscordId} pending={player.userDiscordId === room.pendingTargetUserDiscordId} />)}{eliminated.map(player => <PlayerToken key={player.userDiscordId} player={player} current={false} pending={false} />)}</div></section>
        <section className="event-log game-glass"><h2>سجل الجولة</h2>{room.actions.length === 0 ? <p className="game-help">لا توجد أحداث بعد.</p> : room.actions.slice(0, 7).map(action => <p className={`event-row ${action.powerUpKey ? 'power-event' : ''}`} key={`${action.createdAt}-${action.actionType}`}>{action.powerUpIcon && <span>{action.powerUpIcon}</span>}{action.message}</p>)}</section>
      </aside>
    </div>}
    {room.status === 'Completed' && <section className="victory-room game-glass">
      <div className="trophy">🏆</div><h1>انتهت لعبة الروليت</h1><div className="winner-card shimmer"><h2>{displayName(room.winner)} هو الفائز!</h2><p>{room.winner?.userDiscordId === identity.userId ? `مبروك! حصلت على ${room.winnerCoins} عملة` : 'حظ أوفر في الجولة القادمة'}</p></div>
      <div className="reward-grid"><div className="reward-tile"><span>مكافأة الفائز</span><strong>{room.winnerCoins} عملة</strong></div><div className="reward-tile"><span>عدد اللاعبين</span><strong>{room.players.length}</strong></div><div className="reward-tile"><span>الجولات</span><strong>{room.currentRound}</strong></div></div>
      <div className="roulette-actions"><button className="game-button primary" onClick={() => navigate('/games')}>العودة لمركز الألعاب</button><button className="game-button gold" onClick={() => navigate('/games/roulette')}>العب مرة أخرى</button><button className="game-button secondary" onClick={() => navigate('/store')}>فتح المتجر</button></div>
    </section>}
  </div></main>;
}
