import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { setActivitiesAccessToken } from './api';

const instances: FakeConnection[] = [];

class FakeConnection {
  state = 'Disconnected';
  handlers = new Map<string, Function>();
  reconnectHandler: Function | null = null;
  reconnectingHandler: Function | null = null;
  closeHandler: Function | null = null;
  starts = 0;
  invokes: Array<{ method: string; arg: string }> = [];
  accessTokenFactory?: () => string;

  on(name: string, handler: Function) { this.handlers.set(name, handler); }
  onreconnected(handler: Function) { this.reconnectHandler = handler; }
  onreconnecting(handler: Function) { this.reconnectingHandler = handler; }
  onclose(handler: Function) { this.closeHandler = handler; }
  async start() { this.state = 'Connected'; this.starts++; }
  async stop() { this.state = 'Disconnected'; }
  async invoke(method: string, arg: string) { this.invokes.push({ method, arg }); }
}

vi.mock('@microsoft/signalr', () => {
  class HubConnectionBuilder {
    private options: { accessTokenFactory?: () => string } = {};
    withUrl(_url: string, options: { accessTokenFactory?: () => string }) { this.options = options; return this; }
    withAutomaticReconnect() { return this; }
    configureLogging() { return this; }
    build() {
      const instance = new FakeConnection();
      instance.accessTokenFactory = this.options.accessTokenFactory;
      instances.push(instance);
      return instance;
    }
  }
  return {
    HubConnectionBuilder,
    HubConnectionState: { Connected: 'Connected', Connecting: 'Connecting', Reconnecting: 'Reconnecting', Disconnected: 'Disconnected' },
    LogLevel: { Information: 2, Warning: 3 }
  };
});

describe('activitiesSignalR', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_ACTIVITIES_API_BASE_URL', 'http://activities.test');
  });

  afterEach(async () => {
    const mod = await import('./activitiesSignalR');
    await mod.disconnectActivitiesGameHub();
    instances.length = 0;
    setActivitiesAccessToken(null);
    vi.resetModules();
  });

  it('supplies the in-memory Activities JWT to SignalR', async () => {
    setActivitiesAccessToken('jwt-token');
    const mod = await import('./activitiesSignalR');

    await mod.connectActivitiesGameHub();

    expect(instances).toHaveLength(1);
    expect(instances[0].accessTokenFactory?.()).toBe('jwt-token');
  });

  it('creates one managed connection and joins trusted Roulette sessions', async () => {
    setActivitiesAccessToken('jwt-token');
    const mod = await import('./activitiesSignalR');

    await mod.connectActivitiesGameHub();
    await mod.connectActivitiesGameHub();
    await mod.joinRouletteGameSession('game-1');

    expect(instances).toHaveLength(1);
    expect(instances[0].starts).toBe(1);
    expect(instances[0].invokes).toContainEqual({ method: 'JoinRouletteGameSession', arg: 'game-1' });
  });

  it('replaces duplicate roulette event handlers and cleans up', async () => {
    setActivitiesAccessToken('jwt-token');
    const mod = await import('./activitiesSignalR');
    await mod.connectActivitiesGameHub();
    const first = vi.fn();
    const second = vi.fn();

    const offFirst = mod.onRouletteEvent('game-1', first);
    const offSecond = mod.onRouletteEvent('game-1', second);
    instances[0].handlers.get('RoulettePlayerJoined')?.({ gameSessionId: 'game-1' });

    expect(first).not.toHaveBeenCalled();
    expect(second).toHaveBeenCalledOnce();

    offFirst();
    offSecond();
    instances[0].handlers.get('RoulettePlayerJoined')?.({ gameSessionId: 'game-1' });
    expect(second).toHaveBeenCalledOnce();
  });

  it('does not run reconnect callbacks during first connect or synthetic reconnected events', async () => {
    setActivitiesAccessToken('jwt-token');
    const mod = await import('./activitiesSignalR');
    const restore = vi.fn();

    const cleanup = mod.onActivitiesReconnected(restore);
    await mod.connectActivitiesGameHub();
    await instances[0].reconnectHandler?.();

    expect(restore).not.toHaveBeenCalled();
    expect(mod.getActivitiesConnectionLifecycle()).toMatchObject({ phase: 'connected', hasConnectedBefore: true });

    cleanup();
  });

  it('runs reconnect callbacks once after a real SignalR reconnect cycle', async () => {
    setActivitiesAccessToken('jwt-token');
    const mod = await import('./activitiesSignalR');
    await mod.connectActivitiesGameHub();
    const restore = vi.fn();

    const cleanup = mod.onActivitiesReconnected(restore);
    await instances[0].reconnectingHandler?.();
    await instances[0].reconnectHandler?.();
    await instances[0].reconnectHandler?.();
    cleanup();
    await instances[0].reconnectingHandler?.();
    await instances[0].reconnectHandler?.();

    expect(restore).toHaveBeenCalledOnce();
  });
});
