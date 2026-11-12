import { Component, computed, input, model, output, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DxFormComponent, DxFormModule } from 'devextreme-angular/ui/form';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { finalize } from 'rxjs';
import { toProblemDetails } from '../../../../core/models/problem-details.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { DEVICE_CONSTRAINTS, Device, SaveDeviceDto } from '../../models/device.model';
import { DeviceTypesService } from '../../services/device-types.service';
import { DevicesService } from '../../services/devices.service';

interface DeviceFormData {
  inventoryNumber: string;
  name: string;
  typeId: string | null;
  room: number | null;
}

@Component({
  selector: 'app-device-dialog',
  imports: [DxPopupModule, DxFormModule],
  templateUrl: './device-dialog.html',
})
export class DeviceDialog {
  public readonly visible = model(false);

  /** The device being edited; `null` opens the dialog for a new one. */
  public readonly device = input<Device | null>(null);

  public readonly saved = output<Device>();

  protected readonly constraints = DEVICE_CONSTRAINTS;

  protected readonly formData = signal<DeviceFormData>(DeviceDialog.toFormData(null));
  protected readonly isSaving = signal(false);

  protected readonly title = computed(() =>
    this.device() ? 'Редактирование техники' : 'Новая техника',
  );

  protected readonly inventoryNumberEditorOptions = {
    maxLength: DEVICE_CONSTRAINTS.inventoryNumberMaxLength,
    validationMessageMode: 'always',
  };

  protected readonly nameEditorOptions = {
    maxLength: DEVICE_CONSTRAINTS.nameMaxLength,
    validationMessageMode: 'always',
  };

  protected readonly roomEditorOptions = {
    min: DEVICE_CONSTRAINTS.roomMin,
    max: DEVICE_CONSTRAINTS.roomMax,
    format: '#',
    showSpinButtons: true,
    validationMessageMode: 'always',
  };

  protected readonly typeEditorOptions = computed(() => ({
    dataSource: this._deviceTypes(),
    valueExpr: 'id',
    displayExpr: 'name',
    placeholder: 'Выберите тип',
    searchEnabled: true,
    validationMessageMode: 'always',
  }));

  protected readonly saveButtonOptions = computed(() => ({
    text: 'Сохранить',
    type: 'default',
    stylingMode: 'contained',
    disabled: this.isSaving(),
    onClick: () => this.save(),
  }));

  protected readonly cancelButtonOptions = {
    text: 'Отмена',
    stylingMode: 'outlined',
    onClick: () => this.visible.set(false),
  };

  private readonly _form = viewChild(DxFormComponent);
  private readonly _deviceTypes;

  /** Inventory number the server rejected as a duplicate; fails validation until changed. */
  private _takenInventoryNumber: string | null = null;

  public constructor(
    private _devicesService: DevicesService,
    private _notificationService: NotificationService,
    deviceTypesService: DeviceTypesService,
  ) {
    this._deviceTypes = toSignal(deviceTypesService.getDeviceTypes(), { initialValue: [] });
  }

  protected readonly isInventoryNumberFree = (options: { value?: string }): boolean =>
    options.value !== this._takenInventoryNumber;

  protected onShowing(): void {
    this._takenInventoryNumber = null;
    this.formData.set(DeviceDialog.toFormData(this.device()));
  }

  protected onShown(): void {
    this._form()?.instance.getEditor('inventoryNumber')?.focus();
  }

  protected save(): void {
    const form = this._form();

    if (!form || this.isSaving()) return;
    if (!form.instance.validate().isValid) return;

    const formData = this.formData();
    const dto: SaveDeviceDto = {
      inventoryNumber: formData.inventoryNumber,
      name: formData.name.trim(),
      typeId: formData.typeId!,
      room: formData.room!,
    };

    const device = this.device();
    const request$ = device
      ? this._devicesService.updateDevice(device.id, dto)
      : this._devicesService.createDevice(dto);

    this.isSaving.set(true);

    request$.pipe(finalize(() => this.isSaving.set(false))).subscribe({
      next: (savedDevice) => {
        this._notificationService.success(
          device
            ? `Техника «${savedDevice.inventoryNumber}» сохранена`
            : `Техника «${savedDevice.inventoryNumber}» добавлена`,
        );

        this.saved.emit(savedDevice);
        this.visible.set(false);
      },
      error: (error) => this.handleError(error, dto),
    });
  }

  private handleError(error: unknown, dto: SaveDeviceDto): void {
    const problem = toProblemDetails(error);

    switch (problem?.code) {
      case 'Device.AlreadyExists':
        this._takenInventoryNumber = dto.inventoryNumber;
        this._form()?.instance.validate();
        break;
      case 'Operation.LimitExceeded':
        this._notificationService.error(
          `Достигнут лимит: в базе не может быть больше ${this.constraints.maxCount} записей`,
        );
        break;
      case 'Device.NotFound':
        this._notificationService.error('Запись не найдена: возможно, её уже удалили');
        break;
      default:
        this._notificationService.error(problem?.detail ?? 'Не удалось сохранить запись');
    }
  }

  private static toFormData(device: Device | null): DeviceFormData {
    return {
      inventoryNumber: device?.inventoryNumber ?? '',
      name: device?.name ?? '',
      typeId: device?.typeId ?? null,
      room: device?.room ?? null,
    };
  }
}
