import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { getStore, purchasePowerUp } from '../lib/api';
import { useActivity } from '../context/ActivityProvider';
import type { PowerUpStore } from '../types';
import './RoulettePage.css';

const friendlyDescription = (key: string, fallback: string) => key === 'shield'
  ? 'يحميك من الإقصاء مرة واحدة.'
  : key === 'reverse'
    ? 'يعكس الإقصاء على اللاعب الذي لف العجلة.'
    : key === 'respin'
      ? 'يمنحك فرصة جديدة بتدوير العجلة مرة أخرى.'
      : fallback;

export function StorePage() {
  const { identity } = useActivity(); const navigate = useNavigate(); const [params] = useSearchParams();
  const [store, setStore] = useState<PowerUpStore | null>(null); const [busyKey, setBusyKey] = useState(''); const [error, setError] = useState(''); const [notice, setNotice] = useState(''); const [purchasedKey, setPurchasedKey] = useState('');
  const returnTo = params.get('returnTo') ?? ''; const safeReturnTo = returnTo.startsWith('/') && !returnTo.startsWith('//') ? returnTo : '';
  const goBack = () => navigate(safeReturnTo || '/games/roulette');
  const load = async () => { if (!identity) return; try { setStore(await getStore(identity.accessToken, identity.guildId)); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر تحميل المتجر.'); } };
  useEffect(() => { void load(); }, [identity]);
  const buy = async (key: string, name: string) => { if (!identity || busyKey) return; setBusyKey(key); setError(''); setNotice(''); try { await purchasePowerUp(identity.accessToken, identity.guildId, key); await load(); setNotice(`تم شراء ${name} بنجاح`); setPurchasedKey(key); window.setTimeout(() => setPurchasedKey(''), 900); } catch (e) { setError(e instanceof Error ? e.message : 'تعذر إتمام عملية الشراء.'); } finally { setBusyKey(''); } };
  if (!store) return <main className="center-state game-page"><div className="loader"/><p>{error || 'جاري تحميل متجر الروليت...'}</p></main>;
  return <main className="roulette-page game-page"><div className="game-shell">
    <section className="store-hero game-glass shimmer"><div className="roulette-title"><span className="roulette-logo">🛒</span><div><span className="game-status">متجر الخصائص</span><h1>جهّز نفسك للتحدي</h1><p className="game-help">خصائص تستخدمها أثناء اللحظات الحاسمة في الروليت. العملات افتراضية داخل اللعبة فقط.</p></div></div><div className="hero-actions"><span className="game-coin">💰 {store.balance} عملة</span><button className="game-button secondary" onClick={goBack}>{safeReturnTo ? 'العودة للعبة' : 'العودة للروليت'}</button></div></section>
    {error && <p className="error-text">{error}</p>}{notice && <p className="toast-game">{notice}</p>}
    <section className="store-grid">{store.items.map(item => {
      const canBuy = item.isEnabledForGuild && store.balance >= item.price;
      return <article className={`store-card game-glass ${!item.isEnabledForGuild ? 'disabled' : ''} ${purchasedKey === item.key ? 'purchased' : ''}`} key={item.key}>
        <div className="store-icon">{item.icon}</div>
        <div><h2>{item.name}</h2><p>{friendlyDescription(item.key, item.description)}</p></div>
        <span className="owned-badge">تملك: {item.ownedQuantity}</span>
        <div className="roulette-topbar"><span className="game-coin">💰 {item.price}</span><small className="game-help">الحد: {item.maxUsesPerGame} في الجولة</small></div>
        <button className={`game-button ${canBuy ? 'gold' : 'secondary'}`} disabled={!canBuy || busyKey === item.key} onClick={() => void buy(item.key, item.name)}>{!item.isEnabledForGuild ? 'غير متاحة' : store.balance < item.price ? 'رصيد غير كافٍ' : busyKey === item.key ? 'جاري الشراء...' : 'شراء'}</button>
      </article>;
    })}</section>
  </div></main>;
}
