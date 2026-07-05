import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useActivity } from '../context/ActivityProvider';
import { getLeaderboard } from '../lib/api';
import type { LeaderboardEntry } from '../types';

export function LeaderboardPage() {
  const { identity } = useActivity(); const navigate = useNavigate(); const [rows, setRows] = useState<LeaderboardEntry[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { if (!identity) return; getLeaderboard(identity.accessToken, identity.guildId, identity.channelId).then(setRows).catch(e => setError(e instanceof Error ? e.message : 'تعذر تحميل الترتيب.')).finally(() => setLoading(false)); }, [identity]);
  return <main className="page compact"><header className="section-head"><div><h1>🏆 الترتيب</h1><p>أفضل اللاعبين في تحدي الأسئلة.</p></div><button className="button secondary" onClick={() => navigate('/games')}>العودة لمركز الألعاب</button></header>{loading ? <section className="empty-card">جاري تحميل الترتيب...</section> : error ? <section className="empty-card error-text">{error}</section> : rows.length === 0 ? <section className="empty-card">لا يوجد لاعبين في الترتيب حتى الآن.</section> : <section className="leaderboard-card">{rows.map((row, i) => <div className="leader-row" key={row.userDiscordId}><span className={`rank rank-${i + 1}`}>{i + 1}</span><div><strong>{row.username}</strong><small>{row.gamesPlayed} لعبة · {row.wins} فوز</small></div><b>{row.totalPoints} نقطة</b></div>)}</section>}</main>;
}
