import type { ActivityContext, CompleteSessionResponse, GameWallet, LeaderboardEntry, PendingRouletteIntent, RouletteRoom, RouletteSpinResult, StartSessionResponse } from '../types';

const configuredBase = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim().replace(/\/$/, '') ?? '';
const url = (path: string) => `${configuredBase}${path}`;

export class ApiError extends Error { constructor(message: string, public status: number) { super(message); } }

async function request<T>(path: string, accessToken?: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  const response = await fetch(url(path), { ...init, headers });
  const body = await response.json().catch(() => null) as { message?: string } | T | null;
  if (!response.ok) throw new ApiError((body as { message?: string } | null)?.message ?? 'تعذر إكمال الطلب الآن.', response.status);
  return body as T;
}

export const exchangeActivityCode = (code: string) => request<{ accessToken: string; expiresIn: number; tokenType: string; scope: string }>('/api/discord/activity/token', undefined, { method: 'POST', body: JSON.stringify({ code }) });
export const getActivityContext = (token: string, guildId: string, channelId: string) => request<ActivityContext>(`/api/games/activity/context?guildDiscordId=${encodeURIComponent(guildId)}&channelDiscordId=${encodeURIComponent(channelId)}`, token);
export const startSession = (token: string, guildDiscordId: string, channelDiscordId: string, gameKey: string) => request<StartSessionResponse>('/api/games/activity/start-session', token, { method: 'POST', body: JSON.stringify({ guildDiscordId, channelDiscordId, gameKey }) });
export const completeSession = (token: string, sessionId: string, guildDiscordId: string, score: number, won: boolean) => request<CompleteSessionResponse>('/api/games/activity/complete-session', token, { method: 'POST', body: JSON.stringify({ sessionId, guildDiscordId, score, won }) });
export const getLeaderboard = (token: string, guildId: string, channelId: string, gameKey = 'quiz') => request<LeaderboardEntry[]>(`/api/games/activity/leaderboard?guildDiscordId=${encodeURIComponent(guildId)}&channelDiscordId=${encodeURIComponent(channelId)}&gameKey=${encodeURIComponent(gameKey)}`, token);
const rouletteScope = (guildDiscordId: string, channelDiscordId: string) => JSON.stringify({ guildDiscordId, channelDiscordId });
export const getWallet = (token: string, guildId: string) => request<GameWallet>(`/api/games/activity/wallet?guildDiscordId=${encodeURIComponent(guildId)}`, token);
export const createRouletteRoom = (token: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>('/api/games/activity/roulette/rooms', token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const getOpenRouletteRooms = (token: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom[]>(`/api/games/activity/roulette/rooms/open?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, token);
export const getRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, token);
export const joinRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/join`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const leaveRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/leave`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const startRouletteRoom = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteRoom>(`/api/games/activity/roulette/rooms/${roomId}/start`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const spinRoulette = (token: string, roomId: string, guildDiscordId: string, channelDiscordId: string) => request<RouletteSpinResult>(`/api/games/activity/roulette/rooms/${roomId}/spin`, token, { method: 'POST', body: rouletteScope(guildDiscordId, channelDiscordId) });
export const consumePendingRouletteIntent = (token: string, guildDiscordId: string, channelDiscordId: string) => request<PendingRouletteIntent | null>(`/api/games/activity/roulette/pending-intent?guildDiscordId=${encodeURIComponent(guildDiscordId)}&channelDiscordId=${encodeURIComponent(channelDiscordId)}`, token);
