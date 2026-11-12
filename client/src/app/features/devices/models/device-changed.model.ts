export type DeviceChangeKind = 'Created' | 'Updated' | 'Deleted';

export interface DeviceChangedNotification {
  deviceId: string;
  kind: DeviceChangeKind;
}
