import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError, completeSession, startSession } from '../lib/api';
import { useActivity } from '../context/ActivityProvider';
import type { CompleteSessionResponse, StartSessionResponse } from '../types';

const questions = [
  { text: 'ما عاصمة السعودية؟', answers: ['الرياض', 'جدة', 'الدمام', 'مكة'], correct: 0 },
  { text: 'كم ناتج 7 × 8؟', answers: ['48', '56', '54', '64'], correct: 1 },
  { text: 'أي كوكب يُعرف بالكوكب الأحمر؟', answers: ['الزهرة', 'عطارد', 'المريخ', 'المشتري'], correct: 2 }
];

export function QuizPage() {
  const { identity } = useActivity(); const navigate = useNavigate(); const started = useRef(false);
  const [session, setSession] = useState<StartSessionResponse | null>(null); const [index, setIndex] = useState(0); const [correct, setCorrect] = useState(0); const [selected, setSelected] = useState<number | null>(null); const [submitting, setSubmitting] = useState(false); const [result, setResult] = useState<CompleteSessionResponse | null>(null); const [error, setError] = useState('');
  useEffect(() => { if (!identity || started.current) return; started.current = true; startSession(identity.accessToken, identity.guildId, identity.channelId, 'quiz').then(setSession).catch(e => setError(e instanceof Error ? e.message : 'تعذر بدء اللعبة.')); }, [identity]);
  const answer = async (answerIndex: number) => {
    if (selected !== null || submitting || !session || !identity) return; setSelected(answerIndex); const nextCorrect = correct + (answerIndex === questions[index].correct ? 1 : 0); setCorrect(nextCorrect);
    if (index < questions.length - 1) { window.setTimeout(() => { setIndex(value => value + 1); setSelected(null); }, 550); return; }
    setSubmitting(true); try { setResult(await completeSession(identity.accessToken, session.sessionId, identity.guildId, nextCorrect * 100, nextCorrect >= 2)); } catch (e) { setError(e instanceof ApiError && e.status === 410 ? 'انتهت جلسة اللعبة. ابدأ من جديد.' : e instanceof Error ? e.message : 'تعذر حفظ النتيجة.'); } finally { setSubmitting(false); }
  };
  if (error) return <main className="page compact"><section className="result-card"><span className="result-icon">⚠️</span><h1>{error}</h1><button className="button primary" onClick={() => navigate('/games')}>العودة لمركز الألعاب</button></section></main>;
  if (!session) return <main className="center-state"><div className="loader"/><h1>جاري بدء تحدي الأسئلة...</h1></main>;
  if (result) { const won = correct >= 2; return <main className="page compact"><section className="result-card"><span className="result-icon">{won ? '🏆' : '🎯'}</span><h1>{won ? 'فزت في تحدي الأسئلة!' : 'انتهى التحدي.'}</h1>{won ? <p>حصلت على <strong>{result.pointsAwarded}</strong> نقطة.</p> : <p>إجاباتك الصحيحة: <strong>{correct}</strong> من {questions.length}</p>}<p>مجموع نقاطك الآن: <strong>{result.player.totalPoints}</strong></p><div className="button-row"><button className="button primary" onClick={() => navigate('/games')}>العودة لمركز الألعاب</button><button className="button secondary" onClick={() => navigate('/leaderboard')}>عرض الترتيب</button></div></section></main>; }
  const question = questions[index]; return <main className="page compact"><header className="quiz-head"><button className="text-button" onClick={() => navigate('/games')}>مركز الألعاب</button><span>السؤال {index + 1} من {questions.length}</span></header><section className="quiz-card"><div className="progress"><i style={{ width: `${((index + 1) / questions.length) * 100}%` }}/></div><h1>تحدي الأسئلة</h1><h2>{question.text}</h2><div className="answers">{question.answers.map((item, answerIndex) => <button key={item} disabled={selected !== null || submitting} className={`answer ${selected === answerIndex ? (answerIndex === question.correct ? 'correct' : 'wrong') : ''}`} onClick={() => answer(answerIndex)}>{item}</button>)}</div>{submitting && <p className="muted">جاري حفظ النتيجة...</p>}</section></main>;
}
