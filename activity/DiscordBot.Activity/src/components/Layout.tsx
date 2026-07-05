import { Outlet } from 'react-router-dom';
import { useActivity } from '../context/ActivityProvider';

export function Layout() {
  const { loading, error } = useActivity();
  if (loading) return <main className="center-state"><div className="loader"/><h1>جاري تجهيز مركز الألعاب...</h1></main>;
  if (error) return <main className="center-state error-state"><span className="state-icon">⚠️</span><h1>تعذر فتح مركز الألعاب.</h1><p>{error}</p></main>;
  return <div className="app-shell"><Outlet /></div>;
}
