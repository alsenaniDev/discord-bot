import * as signalR from '@microsoft/signalr';
import { getActivitiesAccessToken } from './api';

const configuredActivitiesBase = (import.meta.env.VITE_ACTIVITIES_API_BASE_URL as string | undefined)?.trim().replace(/\/$/, '') ?? '';
const rouletteEventNames = ['RouletteSessionUpdated', 'RoulettePlayerJoined', 'RoulettePlayerLeft', 'RouletteRoundStarted', 'RouletteRoundResult', 'RouletteRoundSettled'];

export type ActivitiesGameEventHandler<T = unknown> = (payload: T) => void;
export type ConnectionPhase = 'idle' | 'connecting' | 'connected' | 'disconnected' | 'reconnecting' | 'restored';
type ReconnectHandler = () => Promise<void> | void;

let connection: signalR.HubConnection | null = null;
const rouletteHandlers = new Map<string, ActivitiesGameEventHandler>();
const reconnectHandlers = new Set<ReconnectHandler>();
let reconnectHooksRegistered = false;
let connectionPhase: ConnectionPhase = 'idle';
let hasConnectedBefore = false;
let realReconnectPending = false;

export async function connectActivitiesGameHub(): Promise<signalR.HubConnection> {
  if (!configuredActivitiesBase) throw new Error('لم يتم إعداد رابط Activities API.');
  if (connection?.state === signalR.HubConnectionState.Connected) return connection;
  if (connection?.state === signalR.HubConnectionState.Connecting || connection?.state === signalR.HubConnectionState.Reconnecting) {
    await waitForConnected(connection);
    return connection;
  }

  connectionPhase = 'connecting';
  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${configuredActivitiesBase}/hubs/games`, {
      accessTokenFactory: () => {
        const token = getActivitiesAccessToken();
        if (!token) throw new Error('انتهت صلاحية جلسة Activity الجديدة. افتح مركز الألعاب مرة ثانية.');
        return token;
      }
    })
    .withAutomaticReconnect([0, 1500, 5000, 10000])
    .configureLogging(import.meta.env.DEV ? signalR.LogLevel.Information : signalR.LogLevel.Warning)
    .build();

  for (const eventName of rouletteEventNames) {
    connection.on(eventName, (payload: { gameSessionId?: string; id?: string }) => {
      const gameSessionId = payload?.gameSessionId ?? payload?.id;
      if (!gameSessionId) return;
      rouletteHandlers.get(gameSessionId)?.({ type: eventName, payload });
    });
  }

  if (!reconnectHooksRegistered) {
    reconnectHooksRegistered = true;
    connection.onreconnecting(() => {
      if (hasConnectedBefore) realReconnectPending = true;
      connectionPhase = 'reconnecting';
    });
    connection.onreconnected(async () => {
      const shouldRestore = hasConnectedBefore && realReconnectPending;
      connectionPhase = shouldRestore ? 'restored' : 'connected';
      realReconnectPending = false;
      if (!shouldRestore) return;
      for (const handler of reconnectHandlers) {
        try { await handler(); } catch { /* Individual screens surface reconnect failures. */ }
      }
      connectionPhase = 'connected';
    });
    connection.onclose(() => {
      connectionPhase = 'disconnected';
      realReconnectPending = false;
    });
  }

  await connection.start();
  connectionPhase = 'connected';
  hasConnectedBefore = true;
  return connection;
}

export async function joinActivitySession(activitySessionId: string): Promise<void> {
  const hub = await connectActivitiesGameHub();
  await hub.invoke('JoinActivitySession', activitySessionId);
}

export async function leaveActivitySession(activitySessionId: string): Promise<void> {
  if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;
  await connection.invoke('LeaveActivitySession', activitySessionId);
}

export async function joinRouletteGameSession(gameSessionId: string): Promise<void> {
  const hub = await connectActivitiesGameHub();
  await hub.invoke('JoinRouletteGameSession', gameSessionId);
}

export function onRouletteEvent(gameSessionId: string, handler: ActivitiesGameEventHandler): () => void {
  rouletteHandlers.set(gameSessionId, handler);
  return () => rouletteHandlers.delete(gameSessionId);
}

export function onActivitiesReconnected(handler: ReconnectHandler): () => void {
  reconnectHandlers.add(handler);
  return () => reconnectHandlers.delete(handler);
}

export function getActivitiesConnectionLifecycle() {
  return { phase: connectionPhase, hasConnectedBefore };
}

export async function disconnectActivitiesGameHub(): Promise<void> {
  rouletteHandlers.clear();
  reconnectHandlers.clear();
  reconnectHooksRegistered = false;
  connectionPhase = 'idle';
  hasConnectedBefore = false;
  realReconnectPending = false;
  if (!connection) return;
  const current = connection;
  connection = null;
  await current.stop();
}

async function waitForConnected(hub: signalR.HubConnection): Promise<void> {
  for (let i = 0; i < 50; i++) {
    if (hub.state === signalR.HubConnectionState.Connected) return;
    if (hub.state === signalR.HubConnectionState.Disconnected) break;
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  if (hub.state !== signalR.HubConnectionState.Connected) throw new Error('تعذر الاتصال بتحديثات اللعبة المباشرة.');
}
