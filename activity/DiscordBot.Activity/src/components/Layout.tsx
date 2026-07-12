import { Outlet } from 'react-router-dom';
import { useActivity } from '../context/ActivityProvider';
import { getRuntimeConfigSummary, isActivityDiagnosticsEnabled, type ApiFailureDiagnostic } from '../lib/api';

export function Layout() {
  const { loading, error, diagnostic, refresh, identity } = useActivity();
  if (loading) return <main className="center-state"><div className="loader"/><h1>جاري تجهيز مركز الألعاب...</h1></main>;
  if (error) return <main className="center-state error-state"><span className="state-icon">⚠️</span><h1>تعذر فتح مركز الألعاب.</h1><p>{error}</p><button className="button primary" onClick={() => { void refresh(); }}>إعادة المحاولة</button><ActivityDiagnostics diagnostic={diagnostic} onRetry={refresh} /></main>;
  return <div className="app-shell">
    {identity?.isLocalBrowserMode ? <div className="local-mode-banner">وضع الاختبار المحلي — {identity.username}</div> : null}
    <Outlet />
  </div>;
}

function ActivityDiagnostics({ diagnostic, onRetry }: { diagnostic: ApiFailureDiagnostic | null; onRetry: () => Promise<void> }) {
  if (!diagnostic || !isActivityDiagnosticsEnabled(diagnostic.guildId)) return null;
  const config = getRuntimeConfigSummary();
  const details = {
    failedUrl: diagnostic.url,
    method: diagnostic.method,
    targetService: diagnostic.targetService,
    httpStatus: diagnostic.status ?? null,
    responseReceived: diagnostic.responseReceived,
    message: diagnostic.message,
    platformApiBaseUrl: diagnostic.platformApiBaseUrl,
    platformApiBaseSource: diagnostic.platformApiBaseSource,
    activitiesApiBaseUrl: diagnostic.activitiesApiBaseUrl,
    guildId: diagnostic.guildId,
    channelId: diagnostic.channelId,
    activityInstanceId: diagnostic.activityInstanceId,
    environment: config.environment,
    pilotGuildCount: config.pilotGuildCount,
    correlationId: diagnostic.correlationId
  };
  const copy = async () => {
    await navigator.clipboard?.writeText(JSON.stringify(details, null, 2));
  };
  return <details className="diagnostics-panel">
    <summary>تفاصيل التشخيص</summary>
    <dl>
      <div><dt>الخدمة</dt><dd>{diagnostic.targetService}</dd></div>
      <div><dt>الطلب</dt><dd>{diagnostic.method} {diagnostic.url}</dd></div>
      <div><dt>رمز HTTP</dt><dd>{diagnostic.status ?? 'لم يصل رد من الخادم'}</dd></div>
      <div><dt>Correlation ID</dt><dd>{diagnostic.correlationId}</dd></div>
      <div><dt>Platform API</dt><dd>{diagnostic.platformApiBaseUrl}</dd></div>
      <div><dt>Platform source</dt><dd>{diagnostic.platformApiBaseSource}</dd></div>
      <div><dt>Activities API</dt><dd>{diagnostic.activitiesApiBaseUrl}</dd></div>
      <div><dt>Guild</dt><dd>{diagnostic.guildId ?? 'غير متاح'}</dd></div>
      <div><dt>Channel</dt><dd>{diagnostic.channelId ?? 'غير متاح'}</dd></div>
      <div><dt>Activity Instance</dt><dd>{diagnostic.activityInstanceId ?? 'غير متاح'}</dd></div>
    </dl>
    <div className="button-row">
      <button className="button secondary" onClick={() => { void copy(); }}>نسخ التفاصيل</button>
      <button className="button secondary" onClick={() => { void onRetry(); }}>إعادة المحاولة</button>
    </div>
  </details>;
}
