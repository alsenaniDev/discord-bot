import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { ApiError, getActivityContext, getLastApiFailure, getLocalActivityProfiles, type ApiFailureDiagnostic, type LocalActivityProfile } from '../lib/api';
import { getRequestedLocalProfile, initializeDiscordActivity, isLocalBrowserModeAvailable } from '../lib/discordSdk';
import type { ActivityContext, ActivityIdentity } from '../types';

interface State { identity: ActivityIdentity | null; context: ActivityContext | null; loading: boolean; error: string; diagnostic: ApiFailureDiagnostic | null; refresh: () => Promise<void>; }
const ActivityState = createContext<State | null>(null);

export function ActivityProvider({ children }: { children: ReactNode }) {
  const [identity, setIdentity] = useState<ActivityIdentity | null>(null); const [context, setContext] = useState<ActivityContext | null>(null); const [loading, setLoading] = useState(true); const [error, setError] = useState(''); const [diagnostic, setDiagnostic] = useState<ApiFailureDiagnostic | null>(null);
  const [localProfiles, setLocalProfiles] = useState<LocalActivityProfile[]>([]);
  const load = async (localProfileName?: string) => {
    setLoading(true); setError(''); setDiagnostic(null);
    try {
      const auth = await initializeDiscordActivity(localProfileName);
      const data = await getActivityContext(auth.accessToken, auth.guildId, auth.channelId);
      setIdentity(auth); setContext(data);
    } catch (e) {
      setContext(null);
      const apiDiagnostic = e instanceof ApiError ? e.diagnostic : getLastApiFailure();
      setDiagnostic(apiDiagnostic ?? null);
      setError(e instanceof Error ? e.message : 'تعذر فتح مركز الألعاب.');
    } finally {
      setLoading(false);
    }
  };
  const refresh = async () => {
    if (!identity) return load();
    setLoading(true); setError(''); setDiagnostic(null);
    try {
      setContext(await getActivityContext(identity.accessToken, identity.guildId, identity.channelId));
    } catch (e) {
      const apiDiagnostic = e instanceof ApiError ? e.diagnostic : getLastApiFailure();
      setDiagnostic(apiDiagnostic ?? null);
      setError(e instanceof Error ? e.message : 'تعذر فتح مركز الألعاب.');
    } finally {
      setLoading(false);
    }
  };
  const selectLocalProfile = (profileName: string) => {
    const next = new URL(window.location.href);
    next.searchParams.set('localProfile', profileName);
    window.history.replaceState({}, '', next);
    setLocalProfiles([]);
    void load(profileName);
  };
  useEffect(() => {
    const boot = async () => {
      if (isLocalBrowserModeAvailable() && !getRequestedLocalProfile()) {
        setLoading(true); setError(''); setDiagnostic(null);
        try {
          setLocalProfiles(await getLocalActivityProfiles());
        } catch (e) {
          const apiDiagnostic = e instanceof ApiError ? e.diagnostic : getLastApiFailure();
          setDiagnostic(apiDiagnostic ?? null);
          setError(e instanceof Error ? e.message : 'تعذر تحميل ملفات الاختبار المحلي.');
        } finally {
          setLoading(false);
        }
        return;
      }
      await load();
    };
    void boot();
  }, []);
  const value = useMemo(() => ({ identity, context, loading, error, diagnostic, refresh }), [identity, context, loading, error, diagnostic]);
  if (!loading && !identity && localProfiles.length > 0) {
    return <LocalProfileSelector profiles={localProfiles} onSelect={selectLocalProfile} />;
  }
  return <ActivityState.Provider value={value}>{children}</ActivityState.Provider>;
}
export function useActivity() { const value = useContext(ActivityState); if (!value) throw new Error('ActivityProvider is missing'); return value; }

function LocalProfileSelector({ profiles, onSelect }: { profiles: LocalActivityProfile[]; onSelect: (profileName: string) => void }) {
  return <main className="center-state local-profile-state">
    <div className="local-mode-banner">وضع الاختبار المحلي</div>
    <h1>اختر لاعب الاختبار</h1>
    <p>هذه الملفات مضبوطة من الخادم ومخصصة للتجربة المحلية فقط.</p>
    <div className="button-row">
      {profiles.map(profile => <button key={profile.name} className="button primary" onClick={() => onSelect(profile.name)}>
        {profile.username || profile.name}
      </button>)}
    </div>
  </main>;
}
