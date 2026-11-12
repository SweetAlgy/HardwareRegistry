import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export const RETRY_DELAY_MS = 5000;

/** Builds a new, not yet started connection to the devices hub. */
export function createDevicesHubConnection() {
  return new HubConnectionBuilder()
    .withUrl('/hubs/devices')
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}

/**
 * Calls `onChanged` when the registry may have changed: on a change made by anyone, and after
 * a reconnect, since changes made while offline were missed.
 *
 * @param {() => void} onChanged
 * @param {() => import('@microsoft/signalr').HubConnection} createConnection
 * @returns {() => Promise<void>} Closes the connection.
 */
export function connectDevicesHub(onChanged, createConnection = createDevicesHubConnection) {
  const connection = createConnection();

  let isStopped = false;

  connection.on('deviceChanged', () => onChanged());
  connection.onreconnected(() => onChanged());

  // Automatic reconnect covers dropped connections only, not a failed first start.
  (async () => {
    while (!isStopped) {
      try {
        await connection.start();
        return;
      } catch {
        await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
      }
    }
  })();

  return () => {
    isStopped = true;
    return connection.stop();
  };
}
