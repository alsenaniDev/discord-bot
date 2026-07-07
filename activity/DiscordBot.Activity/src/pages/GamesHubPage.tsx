import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useActivity } from '../context/ActivityProvider';
import type { ActivityGame } from '../types';
import { consumePendingRouletteIntent, getMyActiveRouletteRoom, joinRouletteRoom } from '../lib/api';
import './GamesHubPage.css';

function GameGroup({ title, games, emptyMessage, onPlay }: { title: string; games: ActivityGame[]; emptyMessage: string; onPlay: (game: ActivityGame) => void }) {
  return <section className="game-group"><h2 className="group-title">{title}</h2>{games.length === 0 ? <div className="empty-card">{emptyMessage}</div> : <div className="game-grid">{games.map(game => <article className="game-card" key={game.id}><div className="game-icon">{game.iconUrl ? <img src={game.iconUrl} alt=""/> : '🧠'}</div><div><h2>{game.name}</h2><p>{game.description || 'ابدأ التحدي واجمع النقاط.'}</p></div><button className="button primary" onClick={() => onPlay(game)}>ابدأ اللعب</button></article>)}</div>}</section>;
}

export function GamesHubPage() {
  const { context, identity } = useActivity(); const navigate = useNavigate();
  useEffect(() => { if (!identity) return; let active = true; (async () => { try { const intent = await consumePendingRouletteIntent(identity.accessToken, identity.guildId, identity.channelId); if (!active) return; if (intent?.roomId) { try { await joinRouletteRoom(identity.accessToken, intent.roomId, identity.guildId, identity.channelId); } catch { /* The room page shows the current state if the user was already joined or the room changed. */ } if (active) navigate(`/games/roulette/room/${intent.roomId}`, { replace: true }); return; } const mine = await getMyActiveRouletteRoom(identity.accessToken, identity.guildId, identity.channelId); if (active && mine.hasRoom && mine.roomId) navigate(`/games/roulette/room/${mine.roomId}`, { replace: true }); } catch { /* Keep hub usable if checking current room fails. */ } })(); return () => { active = false; }; }, [identity, navigate]);
  const soloGames = context?.games.filter(game => game.playMode === 'Solo') ?? [];
  const multiplayerGames = context?.games.filter(game => game.playMode === 'Multiplayer') ?? [];
  return <main className="page"><header className="hero"><div><span className="eyebrow">أهلًا {identity?.username}</span><h1>🎮 مركز الألعاب</h1><p>اختر لعبة وابدأ التحدي مع أعضاء السيرفر.</p></div><button className="button secondary" onClick={() => navigate('/leaderboard')}>عرض الترتيب</button></header>
    <GameGroup title="ألعاب فردية" games={soloGames} emptyMessage="لا توجد ألعاب فردية مفعّلة حاليًا." onPlay={game => navigate(game.activityRoute)} />
    <GameGroup title="ألعاب جماعية" games={multiplayerGames} emptyMessage="لا توجد ألعاب جماعية متاحة. تأكد من تفعيل الروليت للسيرفر وأن الباقة Pro أو أعلى." onPlay={game => navigate(game.activityRoute)} />
  </main>;
}
