import { Inject, Injectable, InjectionToken } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, share } from 'rxjs';
import { DeviceChangedNotification } from '../models/device-changed.model';

/** Builds a new, not yet started connection to the devices hub. */
export const DEVICES_HUB_CONNECTION_FACTORY = new InjectionToken<() => HubConnection>(
  'DEVICES_HUB_CONNECTION_FACTORY',
  {
    providedIn: 'root',
    factory: () => () =>
      new HubConnectionBuilder()
        .withUrl('/hubs/devices')
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build(),
  },
);

export const DEVICES_HUB_RETRY_DELAY_MS = 5000;

@Injectable({
  providedIn: 'root',
})
export class DevicesHubService {
  /**
   * Fires when the registry may have changed: on a change made by anyone, and after a reconnect,
   * since changes made while offline were missed. One connection is shared by all subscribers
   * and closed when the last one unsubscribes.
   */
  public readonly changes$: Observable<void>;

  public constructor(
    @Inject(DEVICES_HUB_CONNECTION_FACTORY) createConnection: () => HubConnection,
  ) {
    this.changes$ = new Observable<void>((subscriber) => {
      const connection = createConnection();

      let isStopped = false;

      connection.on('deviceChanged', (_: DeviceChangedNotification) => subscriber.next());
      connection.onreconnected(() => subscriber.next());

      // Automatic reconnect covers dropped connections only, not a failed first start.
      const start = async (): Promise<void> => {
        while (!isStopped) {
          try {
            await connection.start();
            return;
          } catch {
            await new Promise((resolve) => setTimeout(resolve, DEVICES_HUB_RETRY_DELAY_MS));
          }
        }
      };

      void start();

      return () => {
        isStopped = true;
        void connection.stop();
      };
    }).pipe(share());
  }
}
