import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { ApiError, getActivityContext, getLastApiFailure, type ApiFailureDiagnostic } from '../lib/api';
import { initializeDiscordActivity } from '../lib/discordSdk';
import type { ActivityContext, ActivityIdentity } from '../types';

interface State { identity: ActivityIdentity | null; context: ActivityContext | null; loading: boolean; error: string; diagnostic: ApiFailureDiagnostic | null; refresh: () => Promise<void>; }
const ActivityState = createContext<State | null>(null);

export function ActivityProvider({ children }: { children: ReactNode }) {
  const [identity, setIdentity] = useState<ActivityIdentity | null>(null); const [context, setContext] = useState<ActivityContext | null>(null); const [loading, setLoading] = useState(true); const [error, setError] = useState(''); const [diagnostic, setDiagnostic] = useState<ApiFailureDiagnostic | null>(null);
  const load = async () => {
    setLoading(true); setError(''); setDiagnostic(null);
    try {
      const auth = await initializeDiscordActivity();
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
  useEffect(() => { void load(); }, []);
  const value = useMemo(() => ({ identity, context, loading, error, diagnostic, refresh }), [identity, context, loading, error, diagnostic]);
  return <ActivityState.Provider value={value}>{children}</ActivityState.Provider>;
}
export function useActivity() { const value = useContext(ActivityState); if (!value) throw new Error('ActivityProvider is missing'); return value; }
