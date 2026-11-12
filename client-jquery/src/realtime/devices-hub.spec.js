import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { RETRY_DELAY_MS, connectDevicesHub } from './devices-hub.js';

function createFakeConnection() {
  const handlers = new Map();
  let reconnectedHandler;

  return {
    on: vi.fn((method, handler) => handlers.set(method, handler)),
    onreconnected: vi.fn((handler) => (reconnectedHandler = handler)),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    emit: (method, ...args) => handlers.get(method)?.(...args),
    reconnect: () => reconnectedHandler?.(),
  };
}

describe('connectDevicesHub', () => {
  let connection;
  let onChanged;

  beforeEach(() => {
    connection = createFakeConnection();
    onChanged = vi.fn();
  });

  afterEach(() => vi.useRealTimers());

  it('connectDevicesHub_called_startsConnection', () => {
    connectDevicesHub(onChanged, () => connection);

    expect(connection.start).toHaveBeenCalledOnce();
  });

  it('connectDevicesHub_deviceChangedMessage_callsOnChanged', () => {
    connectDevicesHub(onChanged, () => connection);

    connection.emit('deviceChanged', { deviceId: 'd1', kind: 'Updated' });

    expect(onChanged).toHaveBeenCalledOnce();
  });

  it('connectDevicesHub_reconnected_callsOnChangedToCatchUp', () => {
    connectDevicesHub(onChanged, () => connection);

    connection.reconnect();

    expect(onChanged).toHaveBeenCalledOnce();
  });

  it('connectDevicesHub_firstStartFails_retriesAfterDelay', async () => {
    vi.useFakeTimers();
    connection.start.mockRejectedValueOnce(new Error('API is down'));
    connectDevicesHub(onChanged, () => connection);
    await vi.advanceTimersByTimeAsync(0);

    expect(connection.start).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(RETRY_DELAY_MS);

    expect(connection.start).toHaveBeenCalledTimes(2);
  });

  it('disconnect_whileRetrying_stopsConnectionAndRetries', async () => {
    vi.useFakeTimers();
    connection.start.mockRejectedValue(new Error('API is down'));
    const disconnect = connectDevicesHub(onChanged, () => connection);
    await vi.advanceTimersByTimeAsync(0);

    await disconnect();
    await vi.advanceTimersByTimeAsync(RETRY_DELAY_MS * 3);

    expect(connection.stop).toHaveBeenCalledOnce();
    expect(connection.start).toHaveBeenCalledTimes(1);
  });
});
