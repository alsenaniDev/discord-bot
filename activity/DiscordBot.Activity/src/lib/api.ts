import type { ActivityContext, CompleteSessionResponse, GameWallet, LeaderboardEntry, MyActiveRouletteRoom, PendingRouletteIntent, PowerUpStore, PurchasePowerUpResponse, RouletteRoom, RouletteRuntimeCapabilities, RouletteSpinResult, StartSessionResponse } from '../types';

const configuredBase = ((import.meta.env.VITE_API_BASE_URL as string | undefined) || (import.meta.env.VITE_PLATFORM_API_BASE_URL as string | undefined))?.trim().replace(/\/$/, '') ?? '';
const configuredActivitiesBase = (import.meta.env.VITE_ACTIVITIES_API_BASE_URL as string | undefined)?.trim().replace(/\/$/, '') ?? '';
const roulettePilotGuildIds = new Set(((import.meta.env.VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS as string | undefined) ?? '').split(',').map(x => x.trim()).filter(Boolean));
const url = (path: string, base = configuredBase) => `${base}${path}`;
let activitiesAccessToken: string | null = null;

export class ApiError extends Error { constructor(message: string, public status: number) { super(message); } }

async function request<T>(path: string, accessToken?: string, init?: RequestInit, base = configuredBase): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  const response = await fetch(url(path, base), { ...init, headers });
  const raw = await response.text().catch(() => '');
  const body = raw ? tryParseJson(raw) as ({ message?: string; detail?: string; title?: string } | T | null) : null;
  if (!response.ok) {
    const problem = body as { message?: string; detail?: string; title?: string } | null;
    throw new ApiError(problem?.message ?? problem?.detail ?? problem?.title ?? plainError(raw) ?? fallbackError(response.status), response.status);
  }
  return body as T;
}

function tryParseJson(value: string): unknown {
  try { return JSON.parse(value); } catch { return null; }
}

function plainError(value: string): string | null {
  const clean = value.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
  return clean && clean.length < 240 ? clean : null;
}

function fallbackError(status: number): string {
  if (status === 403) return 'لا يمكن فتح مركز الألعاب هنا. تأكد أن الألعاب مفعّلة وأنك داخل روم الألعاب المحدد لهذا السيرفر.';
  if (status === 401) return 'انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية.';
  if (status === 404) return 'هذا السيرفر غير مربوط بمنصة البوت أو لم يتم العثور على الإعدادات.';
  return `تعذر إكمال الطلب الآن. رمز HTTP: ${status}`;
}

export const exchangeActivityCode = (code: string) => request<{ accessToken: string; expiresIn: number; tokenType: string; scope: string }>('/api/discord/activity/token', undefined, { method: 'POST', body: JSON.stringify({ code }) });
export const exchangeActivitiesCode = (code: string, guildDiscordId: string, channelDiscordId: string, activityInstanceId?: string | null) => request<{
  accessToken: string;
  expiresAt: string;
  discordAccessToken?: string | null;
  discordExpiresIn?: number;
  discordTokenType?: string | null;
  discordScope?: string | null;
  user: { discordUserId: string; username: string; avatarUrl?: string | null };
}>('/api/auth/discord/exchange', undefined, { method: 'POST', body: JSON.stringify({ code, guildDiscordId, channelDiscordId, activityInstanceId }) }, configuredActivitiesBase);
export const setActivitiesAccessToken = (token?: string | null) => { activitiesAccessToken = token?.trim() || null; };
export const getActivitiesAccessToken = () => activitiesAccessToken;
export const getActivityContext = (token: string, guildId: string, channelId: string) => request<ActivityContext>(`/api/games/activity/context?guildDiscordId=${encodeURIComponent(guildId)}&channelDiscordId=${encodeURIComponent(channelId)}`, token);
export const startSession = (token: string, guildDiscordId: string, channelDiscordId: string, gameKey: string) => request<StartSessionResponse>('/api/games/activity/start-session', token, { method: 'POST', body: JSON.stringify({ guildDiscordId, channelDiscordId, gameKey }) });
export const completeSession = (token: string, sessionId: string, guildDiscordId: string, score: number, won: boolean) => request<CompleteSessionResponse>('/api/games/activity/complete-session', token, { method: 'POST', body: JSON.stringify({ sessionId, guildDiscordId, score, won }) });
export const getLeaderboard = (token: string, guildId: string, channelId: string, gameKey = 'quiz') => request<LeaderboardEntry[]>(`/api/games/activity/leaderboard?guildDiscordId=${encodeURIComponent(guildId)}&channelDiscordId=${encodeURIComponent(channelId)}&gameKey=${encodeURIComponent(gameKey)}`, token);
const rouletteScope = (guildDiscordId: string, channelDiscordId: string) => JSON.stringify({ guildDiscordId, channelDiscordId });
const rouletteRuntime = (guildDiscordId: string): 'legacy' | 'activities' => configuredActivitiesBase && roulettePilotGuildIds.has(guildDiscordId) ? 'activities' : 'legacy';
const rouletteRequest = <T>(token: string, guildDiscordId: string, legacyPath: string, activitiesPath: string, init?: RequestInit) => {
  if (rouletteRuntime(guildDiscordId) !== 'activities') return request<T>(legacyPath, token, init);
  if (!activitiesAccessToken) throw new ApiError('انتهت صلاحية جلسة Activity الجديدة. افتح مركز الألعاب مرة ثانية.', 401);
  return request<T>(activitiesPath, activitiesAccessToken, init, configuredActivitiesBase);
};
export const getWallet = (token: string, guildId: string) => request<GameWallet>(`/api/games/activity/wallet?guildDiscordId=${encodeURIComponent(guildId)}`, token);
export const getStore = (token: string, guildId: string) => request<PowerUpStore>(`/api/games/activity/store?guildDiscordId=${encodeURIComponent(guildId)}`, token);
export const purchasePowerUp = (token: string, guildDiscordId: string, powerUpKey: string) => request<PurchasePowerUpResponse>('/api/games/activity/store/purchase', token, { method: 'POST', body: JSON.stringify({ guildDiscordId, powerUpKey }) });
export const createRouletteRoom = (token: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteRoom>(token, guildDiscordId, '/api/games/activity/roulette/rooms', '/api/roulette/sessions', { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const getOpenRouletteRooms = (token: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteRoom[]>(token, guildDiscordId, `/api/games/activity/roulette/rooms/open?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, `/api/roulette/sessions/open?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`);
export const getMyActiveRouletteRoom = (token: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<MyActiveRouletteRoom>(token, guildDiscordId, `/api/games/activity/roulette/my-active-room?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, `/api/roulette/sessions/my-active?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`);
export const getRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteRoom>(token, guildDiscordId, `/api/games/activity/roulette/rooms/${roomId}?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, `/api/roulette/sessions/${roomId}?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`);
export const reconnectRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteRoom>(token, guildDiscordId, `/api/games/activity/roulette/rooms/${roomId}?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, `/api/roulette/sessions/${roomId}/reconnect`, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const joinRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteRoom>(token, guildDiscordId, `/api/games/activity/roulette/rooms/${roomId}/join`, `/api/roulette/sessions/${roomId}/join`, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const leaveRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteRoom>(token, guildDiscordId, `/api/games/activity/roulette/rooms/${roomId}/leave`, `/api/roulette/sessions/${roomId}/leave`, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const startRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteRoom>(token, guildDiscordId, `/api/games/activity/roulette/rooms/${roomId}/start`, `/api/roulette/sessions/${roomId}/rounds/start`, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const spinRoulette = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteSpinResult>(token, guildDiscordId, `/api/games/activity/roulette/rooms/${roomId}/spin`, `/api/roulette/sessions/${roomId}/spin`, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const useRoulettePowerUp = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string, powerUpKey: string) => {
  if (rouletteRuntime(guildDiscordId) === 'activities') throw new ApiError('الخصائص غير متاحة في تجربة الروليت الجديدة حاليًا. سيتم إبقاء السيرفرات غير الجاهزة على المسار القديم.', 501);
  return request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/use-power-up`, token, { method: 'POST', body: JSON.stringify({ guildDiscordId, channelDiscordId, powerUpKey }) });
};
export const resolveRoulettePendingAction = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => rouletteRequest<RouletteRoom>(token, guildDiscordId, `/api/games/activity/roulette/rooms/${roomId}/resolve-pending-action`, `/api/roulette/sessions/${roomId}/resolve-pending-action`, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const consumePendingRouletteIntent = (token: string, guildDiscordId: string, channelDiscordId: string) => request<PendingRouletteIntent | null>(`/api/games/activity/roulette/pending-intent?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, token);
export const getRouletteCapabilities = (token: string, guildDiscordId: string) =>
  rouletteRuntime(guildDiscordId) === 'activities'
    ? request<RouletteRuntimeCapabilities>('/api/roulette/capabilities', activitiesAccessToken ?? token, undefined, configuredActivitiesBase)
    : Promise.resolve({ runtimeVersion: 'legacy', supportsWalletBets: true, supportsPowerUps: true, supportsReconnect: false });
