import { useNavigate } from 'react-router-dom';
import { useActivity } from '../context/ActivityProvider';

export function GamesHubPage() {
  const { context, identity } = useActivity(); const navigate = useNavigate();
  return <main className="page"><header className="hero"><div><span className="eyebrow">أهلًا {identity?.username}</span><h1>🎮 مركز الألعاب</h1><p>اختر لعبة وابدأ التحدي مع أعضاء السيرفر.</p></div><button className="button secondary" onClick={() => navigate('/leaderboard')}>عرض الترتيب</button></header>
    {!context?.games.length ? <section className="empty-card">لا توجد ألعاب مفعّلة حاليًا.</section> : <section className="game-grid">{context.games.map(game => <article className="game-card" key={game.id}><div className="game-icon">{game.iconUrl ? <img src={game.iconUrl} alt=""/> : '🧠'}</div><div><h2>{game.name}</h2><p>{game.description || 'ابدأ التحدي واجمع النقاط.'}</p></div><button className="button primary" onClick={() => navigate(game.activityRoute)}>ابدأ اللعب</button></article>)}</section>}
  </main>;
}
