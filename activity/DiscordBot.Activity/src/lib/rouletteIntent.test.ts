import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('roulette pending join intent startup', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_ACTIVITIES_API_BASE_URL', 'http://activities.test');
    vi.stubEnv('VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS', '1521518056852029440');
    vi.stubEnv('VITE_API_BASE_URL', '/api');
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
    vi.resetModules();
  });

  it('single-flights duplicate pending intent consumption for StrictMode/remount startup', async () => {
    const fetchMock = vi.fn(async () => new Response(JSON.stringify({ roomId: 'room-1', gameSessionId: 'room-1' }), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);
    const api = await import('./api');
    api.setActivitiesAccessToken('activities-jwt');
    api.setActivityRequestContext({ guildId: '1521518056852029440', channelId: '1523998706331029574', activityInstanceId: 'activity-instance-a' });

    const first = api.consumePendingRouletteIntent('platform-jwt', '1521518056852029440', '1523998706331029574');
    const second = api.consumePendingRouletteIntent('platform-jwt', '1521518056852029440', '1523998706331029574');

    expect(second).toBe(first);
    await expect(first).resolves.toEqual({ roomId: 'room-1', gameSessionId: 'room-1' });
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it('surfaces structured join intent failures without converting them to success', async () => {
    const fetchMock = vi.fn(async () => new Response(JSON.stringify({ code: 'roulette_join_intent_already_consumed', message: 'تم استخدام رابط الانضمام مسبقًا.', correlationId: 'c-1' }), { status: 409 }));
    vi.stubGlobal('fetch', fetchMock);
    const api = await import('./api');
    api.setActivitiesAccessToken('activities-jwt');
    api.setActivityRequestContext({ guildId: '1521518056852029440', channelId: '1523998706331029574', activityInstanceId: 'activity-instance-a' });

    await expect(api.consumePendingRouletteIntent('platform-jwt', '1521518056852029440', '1523998706331029574')).rejects.toMatchObject({
      status: 409,
      message: 'تم استخدام رابط الانضمام مسبقًا.'
    });
  });
});
