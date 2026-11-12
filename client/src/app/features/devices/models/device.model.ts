export interface Device {
  id: string;
  createdAt: string;
  updatedAt: string;
  inventoryNumber: string;
  name: string;
  typeId: string;
  typeName: string;
  room: number;
}

export interface SaveDeviceDto {
  inventoryNumber: string;
  name: string;
  typeId: string;
  room: number;
}

export type DeviceSortField = 'inventoryNumber' | 'typeName' | 'name' | 'room';

export type SortOrder = 'ascending' | 'descending';

export interface DevicesQuery {
  page: number;
  pageSize: number;
  sortBy: DeviceSortField;
  sortOrder: SortOrder;
}

export const DEVICE_CONSTRAINTS = {
  maxCount: 2000,
  inventoryNumberMaxLength: 32,
  inventoryNumberPattern: /^[A-Za-z0-9]+$/,
  nameMaxLength: 256,
  roomMin: 1,
  roomMax: 1000,
} as const;
