import type { ActivityContext, CompleteSessionResponse, LeaderboardEntry, StartSessionResponse } from '../types';

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
