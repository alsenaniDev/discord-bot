import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getStore, purchasePowerUp } from '../lib/api';
import { useActivity } from '../context/ActivityProvider';
import type { PowerUpStore } from '../types';
import './RoulettePage.css';

export function StorePage() {
  const { identity } = useActivity(); const navigate = useNavigate();
  const [store, setStore] = useState<PowerUpStore | null>(null); const [busyKey, setBusyKey] = useState(''); const [error, setError] = useState(''); const [notice, setNotice] = useState('');
  const load = async () => { if (!identity) return; try { setStore(await getStore(identity.accessToken, identity.guildId)); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر تحميل المتجر.'); } };
  useEffect(() => { void load(); }, [identity]);
  const buy = async (key: string) => { if (!identity || busyKey) return; setBusyKey(key); setError(''); setNotice(''); try { await purchasePowerUp(identity.accessToken, identity.guildId, key); await load(); setNotice('تمت إضافة الخاصية إلى مخزونك.'); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر إتمام عملية الشراء.'); } finally { setBusyKey(''); } };
  if (!store) return <main className="center-state"><div className="loader"/><p>{error || 'جاري تحميل متجر الروليت...'}</p></main>;
  return <main className="page"><header className="hero"><div><span className="eyebrow">عملات افتراضية فقط</span><h1>🛒 متجر الروليت</h1><p>اشترِ خصائص تساعدك أثناء الدور المعلّق. لا توجد أي عمليات شراء حقيقية.</p></div><button className="button secondary" onClick={() => navigate('/games/roulette')}>العودة للروليت</button></header>
    <section className="roulette-summary"><div className="wallet-card"><span>رصيدي</span><strong>{store.balance} عملة</strong></div></section>
    {error && <p className="error-text">{error}</p>}{notice && <p className="success-text">{notice}</p>}
    <section className="store-grid">{store.items.map(item => <article className={`store-card ${!item.isEnabledForGuild ? 'disabled' : ''}`} key={item.key}><div className="store-icon">{item.icon}</div><div><h2>{item.name}</h2><p>{item.description}</p><small>المخزون: {item.ownedQuantity} · الحد في الجولة: {item.maxUsesPerGame}</small></div><button className="button primary" disabled={!item.isEnabledForGuild || busyKey === item.key || store.balance < item.price} onClick={() => void buy(item.key)}>{busyKey === item.key ? 'جاري الشراء...' : `${item.price} عملة`}</button></article>)}</section>
  </main>;
}
