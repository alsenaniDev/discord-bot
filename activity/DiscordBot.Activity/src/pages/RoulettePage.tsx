import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { consumePendingRouletteIntent, createRouletteRoom, getMyActiveRouletteRoom, getOpenRouletteRooms, getWallet, joinRouletteRoom } from '../lib/api';
import { useActivity } from '../context/ActivityProvider';
import type { RouletteRoom } from '../types';
import './RoulettePage.css';

const initials = (value: string) => value.trim().slice(0, 2) || '🎡';

export function RoulettePage() {
  const { identity } = useActivity(); const navigate = useNavigate();
  const [rooms, setRooms] = useState<RouletteRoom[]>([]); const [balance, setBalance] = useState(0); const [busy, setBusy] = useState(false); const [checking, setChecking] = useState(true); const [error, setError] = useState(''); const [warning, setWarning] = useState('');
  const load = async () => { if (!identity) return; try { const [open, wallet] = await Promise.all([getOpenRouletteRooms(identity.accessToken, identity.guildId, identity.channelId), getWallet(identity.accessToken, identity.guildId)]); setRooms(open); setBalance(wallet.balance); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر تحميل الروليت.'); } };
  useEffect(() => { if (!identity) return; let active = true; (async () => { let redirected = false; setChecking(true); setWarning(''); try { const intent = await consumePendingRouletteIntent(identity.accessToken, identity.guildId, identity.channelId); if (!active) return; if (intent?.roomId) { try { await joinRouletteRoom(identity.accessToken, intent.roomId, identity.guildId, identity.channelId); } catch { /* room page will show current state */ } redirected = true; navigate(`/games/roulette/room/${intent.roomId}`, { replace: true, state: { entryMode: 'InitialJoin' } }); return; } const mine = await getMyActiveRouletteRoom(identity.accessToken, identity.guildId, identity.channelId); if (!active) return; if (mine.hasRoom && mine.resumeAllowed !== false && mine.roomId) { redirected = true; navigate(`/games/roulette/room/${mine.roomId}`, { replace: true, state: { entryMode: 'InitialLoad' } }); return; } if (mine.resumeAllowed === false && mine.resumeReason && mine.resumeReason !== 'no_active_session') setWarning(mine.resumeReason === 'expired_session' ? 'انتهت صلاحية الغرفة السابقة وتمت إعادتك إلى قائمة الغرف.' : 'لم تعد الغرفة السابقة قابلة للاستعادة.'); } catch { if (active) setWarning('تعذر التحقق من الجولة الحالية.'); } finally { if (active && !redirected) { setChecking(false); void load(); } } })(); return () => { active = false; }; }, [identity, navigate]);
  const create = async () => { if (!identity || busy) return; setBusy(true); setError(''); try { const room = await createRouletteRoom(identity.accessToken, identity.guildId, identity.channelId); navigate(`/games/roulette/room/${room.id}`, { state: { entryMode: 'InitialCreate' } }); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر إنشاء الغرفة.'); } finally { setBusy(false); } };
  const join = async (room: RouletteRoom) => { if (!identity || busy) return; if (room.isCurrentUserJoined) { navigate(`/games/roulette/room/${room.id}`, { state: { entryMode: 'InitialLoad' } }); return; } setBusy(true); setError(''); try { await joinRouletteRoom(identity.accessToken, room.id, identity.guildId, identity.channelId); navigate(`/games/roulette/room/${room.id}`, { state: { entryMode: 'InitialJoin' } }); } catch (e) { const message = e instanceof Error ? e.message : ''; if (message.includes('منضم')) { navigate(`/games/roulette/room/${room.id}`, { state: { entryMode: 'InitialLoad' } }); return; } setError(message || 'تعذر الانضمام للغرفة.'); } finally { setBusy(false); } };
  if (checking) return <main className="center-state game-page"><div className="loader"/><p>جاري التحقق من جولتك الحالية...</p></main>;
  return <main className="roulette-page game-page"><div className="game-shell">
    <section className="roulette-hero game-glass shimmer">
      <div>
        <div className="roulette-title"><span className="roulette-logo">🎡</span><div><span className="game-status">لعبة جماعية</span><h1>الروليت</h1></div></div>
        <p className="game-help">اجمع اللاعبين، لف العجلة، واستخدم الخصائص في اللحظة المناسبة.</p>
        <div className="hero-actions"><button className="game-button primary" disabled={busy} onClick={create}>{busy ? 'جاري تجهيز الغرفة...' : 'إنشاء تحدي جديد'}</button><button className="game-button secondary" onClick={() => navigate('/store')}>🛒 المتجر</button><button className="game-button secondary" onClick={() => void load()}>تحديث الغرف</button></div>
      </div>
      <div className="roulette-summary-grid">
        <div className="summary-tile"><span>رصيدك</span><strong>💰 {balance} عملة</strong></div>
        <div className="summary-tile"><span>الغرف المفتوحة</span><strong>{rooms.length}</strong></div>
        <div className="summary-tile"><span>النمط</span><strong>تحدي جماعي</strong></div>
        <div className="summary-tile"><span>الجوائز</span><strong>عملات افتراضية</strong></div>
      </div>
    </section>
    {warning && <p className="game-help">{warning}</p>}{error && <p className="error-text">{error}</p>}
    <section className="room-list">
      <div className="section-head"><div><h2>الغرف المفتوحة</h2><p className="game-help">ادخل تحدي ينتظر لاعبين الآن.</p></div><span className="game-coin">🔥 مباشر</span></div>
      {rooms.length === 0 ? <div className="empty-arcade game-glass">لا توجد غرف مفتوحة حاليًا. كن أول من يفتح التحدي!</div> : rooms.map(room => <article className="room-card game-glass" key={room.id}>
        <div className="roulette-title"><span className="game-avatar">{initials(room.hostUsername)}</span><div><h2>غرفة {room.hostUsername}</h2><p>👑 المضيف · اللاعبون {room.players.length}/{room.maxPlayers} · الحد الأدنى {room.minPlayers}</p></div></div>
        <button className="game-button gold" disabled={busy} onClick={() => void join(room)}>{room.isCurrentUserJoined ? 'العودة للغرفة' : 'ادخل التحدي'}</button>
      </article>)}
    </section>
  </div></main>;
}
