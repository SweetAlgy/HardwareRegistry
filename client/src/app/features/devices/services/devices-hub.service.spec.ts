import { TestBed } from '@angular/core/testing';
import { HubConnection } from '@microsoft/signalr';
import { Mock } from 'vitest';
import {
  DEVICES_HUB_CONNECTION_FACTORY,
  DEVICES_HUB_RETRY_DELAY_MS,
  DevicesHubService,
} from './devices-hub.service';

interface FakeConnection {
  on: Mock;
  onreconnected: Mock;
  start: Mock;
  stop: Mock;
  emit(method: string, ...args: unknown[]): void;
  reconnect(): void;
}

function createFakeConnection(): FakeConnection {
  const handlers = new Map<string, (...args: unknown[]) => void>();
  let reconnectedHandler: (() => void) | undefined;

  return {
    on: vi.fn((method: string, handler: (...args: unknown[]) => void) =>
      handlers.set(method, handler),
    ),
    onreconnected: vi.fn((handler: () => void) => (reconnectedHandler = handler)),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    emit: (method, ...args) => handlers.get(method)?.(...args),
    reconnect: () => reconnectedHandler?.(),
  };
}

describe('DevicesHubService', () => {
  let connection: FakeConnection;
  let createConnection: Mock;
  let service: DevicesHubService;

  beforeEach(() => {
    connection = createFakeConnection();
    createConnection = vi.fn(() => connection as unknown as HubConnection);

    TestBed.configureTestingModule({
      providers: [{ provide: DEVICES_HUB_CONNECTION_FACTORY, useValue: createConnection }],
    });

    service = TestBed.inject(DevicesHubService);
  });

  afterEach(() => vi.useRealTimers());

  it('changes$_noSubscribers_doesNotConnect', () => {
    expect(createConnection).not.toHaveBeenCalled();
  });

  it('changes$_subscribed_startsConnection', () => {
    const subscription = service.changes$.subscribe();

    expect(connection.start).toHaveBeenCalledOnce();
    subscription.unsubscribe();
  });

  it('changes$_deviceChangedMessage_emits', () => {
    const listener = vi.fn();
    const subscription = service.changes$.subscribe(listener);

    connection.emit('deviceChanged', { deviceId: 'd1', kind: 'Created' });

    expect(listener).toHaveBeenCalledOnce();
    subscription.unsubscribe();
  });

  it('changes$_reconnected_emitsToCatchUpOnMissedChanges', () => {
    const listener = vi.fn();
    const subscription = service.changes$.subscribe(listener);

    connection.reconnect();

    expect(listener).toHaveBeenCalledOnce();
    subscription.unsubscribe();
  });

  it('changes$_twoSubscribers_shareOneConnection', () => {
    const first = vi.fn();
    const second = vi.fn();
    const subscriptions = [service.changes$.subscribe(first), service.changes$.subscribe(second)];

    connection.emit('deviceChanged', { deviceId: 'd1', kind: 'Deleted' });

    expect(createConnection).toHaveBeenCalledOnce();
    expect(first).toHaveBeenCalledOnce();
    expect(second).toHaveBeenCalledOnce();
    subscriptions.forEach((subscription) => subscription.unsubscribe());
  });

  it('changes$_lastSubscriberLeaves_stopsConnection', () => {
    const first = service.changes$.subscribe();
    const second = service.changes$.subscribe();

    first.unsubscribe();
    expect(connection.stop).not.toHaveBeenCalled();

    second.unsubscribe();
    expect(connection.stop).toHaveBeenCalledOnce();
  });

  it('changes$_firstStartFails_retriesAfterDelay', async () => {
    vi.useFakeTimers();
    connection.start.mockRejectedValueOnce(new Error('API is down'));
    const subscription = service.changes$.subscribe();
    await vi.advanceTimersByTimeAsync(0);

    expect(connection.start).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(DEVICES_HUB_RETRY_DELAY_MS);

    expect(connection.start).toHaveBeenCalledTimes(2);
    subscription.unsubscribe();
  });

  it('changes$_unsubscribedWhileRetrying_stopsRetrying', async () => {
    vi.useFakeTimers();
    connection.start.mockRejectedValue(new Error('API is down'));
    const subscription = service.changes$.subscribe();
    await vi.advanceTimersByTimeAsync(0);

    subscription.unsubscribe();
    await vi.advanceTimersByTimeAsync(DEVICES_HUB_RETRY_DELAY_MS * 3);

    expect(connection.start).toHaveBeenCalledTimes(1);
  });
});
