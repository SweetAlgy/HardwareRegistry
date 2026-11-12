import { Component, computed, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DxDataGridComponent, DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { LoadOptions } from 'devextreme/common/data';
import CustomStore from 'devextreme/data/custom_store';
import { ColumnButtonClickEvent, RowDblClickEvent } from 'devextreme/ui/data_grid';
import { confirm } from 'devextreme/ui/dialog';
import { debounceTime, firstValueFrom, forkJoin } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { toPageWindow } from '../../../../core/utils/page-window';
import {
  DEVICE_CONSTRAINTS,
  Device,
  DeviceSortField,
  DevicesQuery,
  SortOrder,
} from '../../models/device.model';
import { DevicesHubService } from '../../services/devices-hub.service';
import { DevicesService } from '../../services/devices.service';
import { DeviceDialog } from '../device-dialog/device-dialog';

@Component({
  selector: 'app-devices-table',
  imports: [DxDataGridModule, DeviceDialog],
  templateUrl: './devices-table.html',
  styleUrl: './devices-table.css',
})
export class DevicesTable {
  protected readonly maxCount = DEVICE_CONSTRAINTS.maxCount;
  protected readonly pageSizes = [10, 20, 50, 100];
  protected readonly defaultPageSize = 20;

  protected readonly dataSource: CustomStore<Device, string>;

  protected readonly totalCount = signal(0);

  protected readonly addButtonOptions = computed(() => ({
    text: 'Добавить технику',
    icon: 'plus',
    type: 'default',
    stylingMode: 'contained',
    disabled: this.totalCount() >= this.maxCount,
    onClick: () => this.openCreateDialog(),
  }));

  protected readonly isDialogVisible = signal(false);
  protected readonly editedDevice = signal<Device | null>(null);

  private readonly _grid = viewChild.required(DxDataGridComponent);

  private readonly _defaultSortField: DeviceSortField = 'inventoryNumber';

  /** Bulk changes arrive as a burst of events; one reload covers them all. */
  private readonly _refreshDebounceMs = 300;

  public constructor(
    private _devicesService: DevicesService,
    private _notificationService: NotificationService,
    devicesHubService: DevicesHubService,
  ) {
    this.dataSource = new CustomStore<Device, string>({
      key: 'id',
      load: (loadOptions) => this.loadDevices(loadOptions),
    });

    devicesHubService.changes$
      .pipe(debounceTime(this._refreshDebounceMs), takeUntilDestroyed())
      .subscribe(() => void this._grid().instance.refresh());
  }

  protected readonly onEditClick = (event: ColumnButtonClickEvent<Device, string>): void => {
    if (event.row) this.openEditDialog(event.row.data);
  };

  protected readonly onDeleteClick = (event: ColumnButtonClickEvent<Device, string>): void => {
    if (event.row) void this.deleteDevice(event.row.data);
  };

  protected onRowDblClick(event: RowDblClickEvent<Device, string>): void {
    if (event.rowType === 'data') this.openEditDialog(event.data);
  }

  protected onDeviceSaved(): void {
    void this._grid().instance.refresh();
  }

  private openCreateDialog(): void {
    this.editedDevice.set(null);
    this.isDialogVisible.set(true);
  }

  private openEditDialog(device: Device): void {
    this.editedDevice.set(device);
    this.isDialogVisible.set(true);
  }

  private async loadDevices(
    loadOptions: LoadOptions<Device>,
  ): Promise<{ data: Device[]; totalCount: number }> {
    const skip = loadOptions.skip ?? 0;
    const take = loadOptions.take ?? this.defaultPageSize;
    const sorting = this.toSorting(loadOptions.sort);
    const { pages, offset } = toPageWindow(skip, take);

    try {
      const devicesPages = await firstValueFrom(
        forkJoin(pages.map((page) => this._devicesService.getDevices({ ...page, ...sorting }))),
      );

      const totalCount = devicesPages[0].totalCount;
      const devices = devicesPages.flatMap((devicesPage) => devicesPage.items);

      this.totalCount.set(totalCount);

      return { data: devices.slice(offset, offset + take), totalCount };
    } catch (error) {
      this._notificationService.error('Не удалось загрузить список техники');
      throw error;
    }
  }

  /** The grid sorts by a single column, so only the first sort descriptor matters. */
  private toSorting(sort: LoadOptions<Device>['sort']): Pick<DevicesQuery, 'sortBy' | 'sortOrder'> {
    const descriptor = Array.isArray(sort) ? sort[0] : sort;

    if (descriptor === null || typeof descriptor !== 'object' || !('selector' in descriptor)) {
      return { sortBy: this._defaultSortField, sortOrder: 'ascending' };
    }

    const sortOrder: SortOrder = descriptor.desc ? 'descending' : 'ascending';

    return { sortBy: descriptor.selector as DeviceSortField, sortOrder };
  }

  private async deleteDevice(device: Device): Promise<void> {
    const isConfirmed = await confirm(
      `Удалить технику «${device.inventoryNumber}»?`,
      'Удаление техники',
    );

    if (!isConfirmed) return;

    try {
      await firstValueFrom(this._devicesService.deleteDevice(device.id));
      this._notificationService.success(`Техника «${device.inventoryNumber}» удалена`);
    } catch {
      this._notificationService.error('Не удалось удалить запись');
    }

    await this._grid().instance.refresh();
  }
}
