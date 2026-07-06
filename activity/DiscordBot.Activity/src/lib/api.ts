import type { ActivityContext, CompleteSessionResponse, GameWallet, LeaderboardEntry, PendingRouletteIntent, PowerUpStore, PurchasePowerUpResponse, RouletteRoom, RouletteSpinResult, StartSessionResponse } from '../types';

const configuredBase = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim().replace(/\/$/, '') ?? '';
const url = (path: string) => `${configuredBase}${path}`;

export class ApiError extends Error { constructor(message: string, public status: number) { super(message); } }

async function request<T>(path: string, accessToken?: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  const response = await fetch(url(path), { ...init, headers });
  const body = await response.json().catch(() => null) as { message?: string; detail?: string; title?: string } | T | null;
  if (!response.ok) { const problem = body as { message?: string; detail?: string; title?: string } | null; throw new ApiError(problem?.message ?? problem?.detail ?? problem?.title ?? `تعذر إكمال الطلب الآن. رمز HTTP: ${response.status}`, response.status); }
  return body as T;
}

export const exchangeActivityCode = (code: string) => request<{ accessToken: string; expiresIn: number; tokenType: string; scope: string }>('/api/discord/activity/token', undefined, { method: 'POST', body: JSON.stringify({ code }) });
export const getActivityContext = (token: string, guildId: string, channelId: string) => request<ActivityContext>(`/api/games/activity/context?guildDiscordId=${encodeURIComponent(guildId)}&channelDiscordId=${encodeURIComponent(channelId)}`, token);
export const startSession = (token: string, guildDiscordId: string, channelDiscordId: string, gameKey: string) => request<StartSessionResponse>('/api/games/activity/start-session', token, { method: 'POST', body: JSON.stringify({ guildDiscordId, channelDiscordId, gameKey }) });
export const completeSession = (token: string, sessionId: string, guildDiscordId: string, score: number, won: boolean) => request<CompleteSessionResponse>('/api/games/activity/complete-session', token, { method: 'POST', body: JSON.stringify({ sessionId, guildDiscordId, score, won }) });
export const getLeaderboard = (token: string, guildId: string, channelId: string, gameKey = 'quiz') => request<LeaderboardEntry[]>(`/api/games/activity/leaderboard?guildDiscordId=${encodeURIComponent(guildId)}&channelDiscordId=${encodeURIComponent(channelId)}&gameKey=${encodeURIComponent(gameKey)}`, token);
const rouletteScope = (guildDiscordId: string, channelDiscordId: string) => JSON.stringify({ guildDiscordId, channelDiscordId });
export const getWallet = (token: string, guildId: string) => request<GameWallet>(`/api/games/activity/wallet?guildDiscordId=${encodeURIComponent(guildId)}`, token);
export const getStore = (token: string, guildId: string) => request<PowerUpStore>(`/api/games/activity/store?guildDiscordId=${encodeURIComponent(guildId)}`, token);
export const purchasePowerUp = (token: string, guildDiscordId: string, powerUpKey: string) => request<PurchasePowerUpResponse>('/api/games/activity/store/purchase', token, { method: 'POST', body: JSON.stringify({ guildDiscordId, powerUpKey }) });
export const createRouletteRoom = (token: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>('/api/games/activity/roulette/rooms', token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const getOpenRouletteRooms = (token: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom[]>(`/api/games/activity/roulette/rooms/open?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, token);
export const getRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, token);
export const joinRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/join`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const leaveRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/leave`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const startRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/start`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const spinRoulette = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteSpinResult>(`/api/games/activity/roulette/rooms/${roomId}/spin`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const useRoulettePowerUp = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string, powerUpKey: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/use-power-up`, token, { method: 'POST', body: JSON.stringify({ guildDiscordId, channelDiscordId, powerUpKey }) });
export const resolveRoulettePendingAction = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/resolve-pending-action`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const consumePendingRouletteIntent = (token: string, guildDiscordId: string, channelDiscordId: string) => request<PendingRouletteIntent | null>(`/api/games/activity/roulette/pending-intent?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, token);
