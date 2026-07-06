import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getRouletteRoom, joinRouletteRoom, leaveRouletteRoom, spinRoulette, startRouletteRoom } from '../lib/api';
import { useActivity } from '../context/ActivityProvider';
import type { RouletteRoom } from '../types';
import './RoulettePage.css';

const statusText: Record<string, string> = { Waiting: 'بانتظار اللاعبين', InProgress: 'اللعبة جارية', Completed: 'اكتملت اللعبة', Cancelled: 'أُلغيت الغرفة', Expired: 'انتهت مدة الانضمام' };

export function RouletteRoomPage() {
  const { roomId = '' } = useParams(); const { identity } = useActivity(); const navigate = useNavigate();
  const [room, setRoom] = useState<RouletteRoom | null>(null); const [busy, setBusy] = useState(false); const [error, setError] = useState(''); const [event, setEvent] = useState(''); const [now, setNow] = useState(Date.now());
  const load = async () => { if (!identity || !roomId) return; try { setRoom(await getRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId)); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر تحميل الغرفة.'); } };
  useEffect(() => { void load(); }, [identity, roomId]);
  useEffect(() => { if (!room || !['Waiting', 'InProgress'].includes(room.status)) return; const id = window.setInterval(() => { setNow(Date.now()); void load(); }, 2500); return () => window.clearInterval(id); }, [room?.status, identity, roomId]);
  const me = useMemo(() => room?.players.find(x => x.userDiscordId === identity?.userId), [room, identity]); const isHost = room?.hostUserDiscordId === identity?.userId;
  const seconds = room ? Math.max(0, Math.ceil((new Date(room.expiresAt).getTime() - now) / 1000)) : 0;
  const act = async (fn: () => Promise<RouletteRoom>, success = '') => { if (busy) return; setBusy(true); setError(''); try { const value = await fn(); setRoom(value); setEvent(success); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر إكمال الطلب.'); } finally { setBusy(false); } };
  const leave = async () => { if (busy || !identity) return; setBusy(true); setError(''); try { await leaveRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId); navigate('/games/roulette', { replace: true }); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر مغادرة الغرفة.'); setBusy(false); } };
  const spin = async () => { if (!identity || busy) return; setBusy(true); setError(''); try { const value = await spinRoulette(identity.accessToken, roomId, identity.guildId, identity.channelId); setRoom(value.room); setEvent(`تم إقصاء ${value.eliminatedPlayer.username}`); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر تدوير الروليت.'); } finally { setBusy(false); } };
  if (!room || !identity) return <main className="center-state"><div className="loader"/><p>{error || 'جاري تحميل غرفة الروليت...'}</p></main>;
  return <main className="page compact"><div className="roulette-room-head"><button className="text-button" onClick={() => navigate('/games/roulette')}>← الروليت</button><span className={`status-pill status-${room.status.toLowerCase()}`}>{statusText[room.status]}</span></div>
    <section className="roulette-stage"><div className="roulette-wheel">🎡</div><h1>{room.status === 'Completed' ? `🏆 الفائز: ${room.winner?.username}` : `الجولة ${room.currentRound || 0}`}</h1>{room.status === 'Completed' ? <p>حصل على {room.winnerCoins} عملة</p> : room.status === 'Waiting' ? <p>متبقي للانضمام: {seconds} ثانية</p> : <p>الروليت يختار لاعبًا عشوائيًا في كل دورة.</p>}{event && <div className="event-banner">{event}</div>}</section>
    {error && <p className="error-text">{error}</p>}<section className="players-card"><div className="room-meta"><span>اللاعبون <b>{room.players.length}/{room.maxPlayers}</b></span><span>الحد الأدنى <b>{room.minPlayers}</b></span></div><div className="player-list">{room.players.map(player => <div className={`player-chip ${player.isAlive ? '' : 'eliminated'}`} key={player.userDiscordId}><span>{player.isAlive ? '🙂' : '💥'} {player.username}</span>{player.isHost && <small>المضيف</small>}</div>)}</div></section>
    <div className="button-row">{!me && room.status === 'Waiting' && <button className="button primary" disabled={busy} onClick={() => void act(() => joinRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId), 'تم الانضمام للجولة')}>انضمام</button>}{isHost && room.status === 'Waiting' && <button className="button primary" disabled={busy || !room.canStart} onClick={() => void act(() => startRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId), 'بدأت اللعبة!')}>بدء اللعبة</button>}{isHost && room.status === 'InProgress' && <button className="button danger" disabled={busy} onClick={() => void spin()}>{busy ? 'جاري التدوير...' : 'تدوير الروليت'}</button>}{me && room.status === 'Waiting' && <button className="button secondary" disabled={busy} onClick={() => void leave()}>مغادرة الغرفة</button>}{room.status === 'Completed' && <><button className="button primary" onClick={() => navigate('/games')}>العودة لمركز الألعاب</button><button className="button secondary" onClick={() => navigate('/leaderboard')}>عرض الترتيب</button></>}</div>
  </main>;
}
