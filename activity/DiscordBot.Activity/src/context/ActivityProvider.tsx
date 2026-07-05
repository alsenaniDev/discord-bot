import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { getActivityContext } from '../lib/api';
import { initializeDiscordActivity } from '../lib/discordSdk';
import type { ActivityContext, ActivityIdentity } from '../types';

interface State { identity: ActivityIdentity | null; context: ActivityContext | null; loading: boolean; error: string; refresh: () => Promise<void>; }
const ActivityState = createContext<State | null>(null);

export function ActivityProvider({ children }: { children: ReactNode }) {
  const [identity, setIdentity] = useState<ActivityIdentity | null>(null); const [context, setContext] = useState<ActivityContext | null>(null); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  const refresh = async () => { if (!identity) return; setContext(await getActivityContext(identity.accessToken, identity.guildId, identity.channelId)); };
  useEffect(() => { let active = true; (async () => { try { const auth = await initializeDiscordActivity(); const data = await getActivityContext(auth.accessToken, auth.guildId, auth.channelId); if (active) { setIdentity(auth); setContext(data); } } catch (e) { if (active) setError(e instanceof Error ? e.message : 'تعذر فتح مركز الألعاب.'); } finally { if (active) setLoading(false); } })(); return () => { active = false; }; }, []);
  const value = useMemo(() => ({ identity, context, loading, error, refresh }), [identity, context, loading, error]);
  return <ActivityState.Provider value={value}>{children}</ActivityState.Provider>;
}
export function useActivity() { const value = useContext(ActivityState); if (!value) throw new Error('ActivityProvider is missing'); return value; }
