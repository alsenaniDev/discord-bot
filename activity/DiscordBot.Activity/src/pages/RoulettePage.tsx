import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createRouletteRoom, getOpenRouletteRooms, getWallet, joinRouletteRoom } from '../lib/api';
import { useActivity } from '../context/ActivityProvider';
import type { RouletteRoom } from '../types';
import './RoulettePage.css';

export function RoulettePage() {
  const { identity } = useActivity(); const navigate = useNavigate();
  const [rooms, setRooms] = useState<RouletteRoom[]>([]); const [balance, setBalance] = useState(0); const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  const load = async () => { if (!identity) return; try { const [open, wallet] = await Promise.all([getOpenRouletteRooms(identity.accessToken, identity.guildId, identity.channelId), getWallet(identity.accessToken, identity.guildId)]); setRooms(open); setBalance(wallet.balance); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر تحميل الروليت.'); } };
  useEffect(() => { void load(); }, [identity]);
  const create = async () => { if (!identity || busy) return; setBusy(true); setError(''); try { const room = await createRouletteRoom(identity.accessToken, identity.guildId, identity.channelId); navigate(`/games/roulette/room/${room.id}`); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر إنشاء الغرفة.'); } finally { setBusy(false); } };
  const join = async (roomId: string) => { if (!identity || busy) return; setBusy(true); setError(''); try { await joinRouletteRoom(identity.accessToken, roomId, identity.guildId, identity.channelId); navigate(`/games/roulette/room/${roomId}`); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر الانضمام للغرفة.'); } finally { setBusy(false); } };
  return <main className="page"><header className="hero"><div><span className="eyebrow">لعبة جماعية</span><h1>🎡 الروليت</h1><p>أنشئ تحديًا جماعيًا وانتظر اللاعبين من روم الألعاب.</p></div><button className="button secondary" onClick={() => navigate('/games')}>مركز الألعاب</button></header>
    <section className="roulette-summary"><div className="wallet-card"><span>رصيدي</span><strong>{balance} عملة</strong></div><button className="button primary" disabled={busy} onClick={create}>{busy ? 'جاري التجهيز...' : 'إنشاء غرفة'}</button><button className="button secondary" onClick={() => void load()}>تحديث الغرف</button></section>
    {error && <p className="error-text">{error}</p>}<section className="room-list"><div className="section-head"><div><h1>الغرف المفتوحة</h1><p>انضم لغرفة تنتظر لاعبين.</p></div></div>{rooms.length === 0 ? <div className="empty-card">لا توجد غرف مفتوحة حاليًا.</div> : rooms.map(room => <article className="room-card" key={room.id}><div><h2>{room.hostUsername}</h2><p>{room.players.length} / {room.maxPlayers} لاعبين · الحد الأدنى {room.minPlayers}</p></div><button className="button primary" disabled={busy} onClick={() => void join(room.id)}>انضمام</button></article>)}</section>
  </main>;
}
