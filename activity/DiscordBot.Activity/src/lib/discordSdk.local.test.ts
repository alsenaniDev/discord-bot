import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const discordSdkConstructor = vi.fn();

vi.mock('@discord/embedded-app-sdk', () => ({
  DiscordSDK: discordSdkConstructor
}));

describe('local browser activity initialization', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_ACTIVITIES_API_BASE_URL', 'http://activities.test');
    vi.stubEnv('VITE_DISCORD_CLIENT_ID', '');
    vi.stubEnv('VITE_ENVIRONMENT', 'development');
    vi.stubGlobal('window', {
      location: new URL('http://localhost:5173/?localProfile=PlayerA')
    });
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
    vi.resetModules();
    discordSdkConstructor.mockReset();
  });

  it('does not instantiate DiscordSDK when frame_id is missing in development local mode', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({
      accessToken: 'local-activities-jwt',
      expiresAt: '2026-07-12T00:00:00Z',
      user: { discordUserId: '900000000000000001', username: 'لاعب A' },
      guildDiscordId: '1521518056852029440',
      channelDiscordId: '1523998706331029574',
      activityInstanceId: 'local-browser-activity'
    }), { status: 200 })));

    const sdk = await import('./discordSdk');
    const identity = await sdk.initializeDiscordActivity();

    expect(discordSdkConstructor).not.toHaveBeenCalled();
    expect(identity).toMatchObject({
      accessToken: 'local-activities-jwt',
      activitiesAccessToken: 'local-activities-jwt',
      guildId: '1521518056852029440',
      channelId: '1523998706331029574',
      activityInstanceId: 'local-browser-activity',
      userId: '900000000000000001',
      isLocalBrowserMode: true,
      localProfileName: 'PlayerA'
    });
  });
}
);
