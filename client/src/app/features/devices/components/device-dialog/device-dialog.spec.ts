import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DxFormComponent } from 'devextreme-angular/ui/form';
import { of, throwError } from 'rxjs';
import { Mock } from 'vitest';
import { userEvent } from 'vitest/browser';
import { NotificationService } from '../../../../core/services/notification.service';
import { Device } from '../../models/device.model';
import { DeviceTypesService } from '../../services/device-types.service';
import { DevicesService } from '../../services/devices.service';
import { DeviceDialog } from './device-dialog';

describe('DeviceDialog', () => {
  const types = [
    { id: 't1', name: 'Монитор' },
    { id: 't2', name: 'Ноутбук' },
  ];

  const device: Device = {
    id: 'd1',
    createdAt: '2026-11-12T00:00:00Z',
    updatedAt: '2026-11-12T00:00:00Z',
    inventoryNumber: 'MON7',
    name: 'Dell P2422H',
    typeId: 't1',
    typeName: 'Монитор',
    room: 305,
  };

  let fixture: ComponentFixture<DeviceDialog>;
  let createDevice: Mock;
  let updateDevice: Mock;
  let notifications: { success: Mock; error: Mock };
  let saved: Mock;

  async function open(edited: Device | null): Promise<void> {
    fixture = TestBed.createComponent(DeviceDialog);
    fixture.componentRef.setInput('device', edited);
    fixture.componentRef.setInput('visible', true);
    saved = vi.fn();
    fixture.componentInstance.saved.subscribe(saved);
    fixture.detectChanges();

    await vi.waitFor(() => expect(input('inventoryNumber')).not.toBeNull());
    await fixture.whenStable();
  }

  /** The input the user types into; some editors keep their `name` on a hidden one. */
  function input(dataField: string): HTMLInputElement | null {
    const editor = fixture.debugElement
      .query(By.directive(DxFormComponent))
      ?.componentInstance.instance.getEditor(dataField);

    return editor?.element().querySelector('input.dx-texteditor-input') ?? null;
  }

  function form(): DxFormComponent {
    return fixture.debugElement.query(By.directive(DxFormComponent)).componentInstance;
  }

  function popupText(): string {
    return document.querySelector('.dx-popup-wrapper')?.textContent ?? '';
  }

  async function fill(card: { inventoryNumber?: string; name?: string; room?: string }) {
    for (const [field, value] of Object.entries(card)) {
      await userEvent.fill(input(field)!, value);
    }
  }

  async function chooseType(typeId: string): Promise<void> {
    form().instance.updateData('typeId', typeId);
    await fixture.whenStable();
  }

  async function clickSave(): Promise<void> {
    const button = [...document.querySelectorAll<HTMLElement>('.dx-popup-wrapper .dx-button')].find(
      (element) => element.textContent?.includes('Сохранить'),
    )!;
    await userEvent.click(button);
  }

  function conflict(code: string, status: number, detail?: string): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: { status, title: 'Error', code, detail } });
  }

  beforeEach(() => {
    createDevice = vi.fn((dto) => of({ ...device, ...dto, id: 'new' }));
    updateDevice = vi.fn((id, dto) => of({ ...device, ...dto, id }));
    notifications = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [DeviceDialog],
      providers: [
        { provide: DevicesService, useValue: { createDevice, updateDevice } },
        { provide: DeviceTypesService, useValue: { getDeviceTypes: () => of(types) } },
        { provide: NotificationService, useValue: notifications },
      ],
    });
  });

  afterEach(() => {
    fixture?.destroy();
    document.querySelectorAll('.dx-overlay-wrapper').forEach((overlay) => overlay.remove());
  });

  it('open_newDevice_showsEmptyCardTitledNew', async () => {
    await open(null);

    expect(popupText()).toContain('Новая техника');
    expect(input('inventoryNumber')!.value).toBe('');
    expect(input('name')!.value).toBe('');
    for (const label of ['Учетный номер', 'Наименование', 'Тип техники', 'Комната размещения']) {
      expect(popupText()).toContain(label);
    }
  });

  it('open_existingDevice_fillsCardWithItsValues', async () => {
    await open(device);

    expect(popupText()).toContain('Редактирование техники');
    expect(input('inventoryNumber')!.value).toBe('MON7');
    expect(input('name')!.value).toBe('Dell P2422H');
    expect(form().instance.option('formData')).toMatchObject({ typeId: 't1', room: 305 });
  });

  it('save_emptyCard_showsRequiredMessagesWithoutCallingApi', async () => {
    await open(null);

    await clickSave();

    await vi.waitFor(() => expect(popupText()).toContain('Укажите учетный номер'));
    expect(popupText()).toContain('Укажите наименование');
    expect(popupText()).toContain('Выберите тип техники');
    expect(popupText()).toContain('Укажите комнату');
    expect(createDevice).not.toHaveBeenCalled();
  });

  it.each(['ПК1', 'PC-1', 'PC 1'])(
    'save_inventoryNumber%j_showsFormatMessage',
    async (inventoryNumber) => {
      await open(null);
      await fill({ inventoryNumber, name: 'Монитор', room: '1' });
      await chooseType('t1');

      await clickSave();

      await vi.waitFor(() => expect(popupText()).toContain('Только цифры и латинские буквы'));
      expect(createDevice).not.toHaveBeenCalled();
    },
  );

  it('editor_inventoryNumber_isLimitedTo32Characters', async () => {
    await open(null);

    expect(input('inventoryNumber')!.maxLength).toBe(32);
    expect(input('name')!.maxLength).toBe(256);
  });

  it('save_roomOfZero_showsRangeMessage', async () => {
    await open(null);
    await fill({ inventoryNumber: 'PC1', name: 'Монитор' });
    await chooseType('t1');
    form().instance.updateData('room', 0);

    await clickSave();

    await vi.waitFor(() => expect(popupText()).toContain('Целое число от 1 до 1000'));
    expect(createDevice).not.toHaveBeenCalled();
  });

  it.each(['0', '1001', '5000', '-3'])(
    'save_typedRoom%s_neverSendsRoomOutsideRange',
    async (room) => {
      await open(null);
      await fill({ inventoryNumber: 'PC1', name: 'Монитор', room });
      await userEvent.tab();
      await chooseType('t1');

      await clickSave();
      await new Promise((resolve) => setTimeout(resolve, 200));

      for (const [dto] of createDevice.mock.calls) {
        expect(dto.room).toBeGreaterThanOrEqual(1);
        expect(dto.room).toBeLessThanOrEqual(1000);
      }
    },
  );

  it('typeEditor_dictionary_offersEveryType', async () => {
    await open(null);

    const editor = form().instance.getEditor('typeId')!;

    expect(editor.option('dataSource')).toEqual(types);
  });

  it('save_validNewCard_createsDeviceWithTrimmedName', async () => {
    await open(null);
    await fill({ inventoryNumber: 'PC1', name: '  Рабочая станция  ', room: '101' });
    await chooseType('t2');

    await clickSave();

    await vi.waitFor(() =>
      expect(createDevice).toHaveBeenCalledWith({
        inventoryNumber: 'PC1',
        name: 'Рабочая станция',
        typeId: 't2',
        room: 101,
      }),
    );
  });

  it('save_validNewCard_notifiesEmitsSavedAndCloses', async () => {
    await open(null);
    await fill({ inventoryNumber: 'PC1', name: 'Рабочая станция', room: '101' });
    await chooseType('t2');

    await clickSave();

    await vi.waitFor(() => expect(saved).toHaveBeenCalledOnce());
    expect(notifications.success).toHaveBeenCalledWith('Техника «PC1» добавлена');
    expect(fixture.componentInstance.visible()).toBe(false);
  });

  it('save_editedDevice_updatesIt', async () => {
    await open(device);
    await fill({ name: 'Dell U2723QE', room: '306' });

    await clickSave();

    await vi.waitFor(() =>
      expect(updateDevice).toHaveBeenCalledWith('d1', {
        inventoryNumber: 'MON7',
        name: 'Dell U2723QE',
        typeId: 't1',
        room: 306,
      }),
    );
    expect(createDevice).not.toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalledWith('Техника «MON7» сохранена');
  });

  it('save_duplicateInventoryNumber_marksFieldUntilChanged', async () => {
    createDevice.mockImplementation(() => throwError(() => conflict('Device.AlreadyExists', 409)));
    await open(null);
    await fill({ inventoryNumber: 'PC1', name: 'Монитор', room: '1' });
    await chooseType('t1');

    await clickSave();

    await vi.waitFor(() =>
      expect(popupText()).toContain('Техника с таким учетным номером уже существует'),
    );
    expect(fixture.componentInstance.visible()).toBe(true);
    expect(saved).not.toHaveBeenCalled();

    await fill({ inventoryNumber: 'PC2' });
    await userEvent.tab();

    await vi.waitFor(() =>
      expect(popupText()).not.toContain('Техника с таким учетным номером уже существует'),
    );
  });

  it('save_limitExceeded_showsLimitNotificationAndStaysOpen', async () => {
    createDevice.mockImplementation(() =>
      throwError(() => conflict('Operation.LimitExceeded', 400)),
    );
    await open(null);
    await fill({ inventoryNumber: 'PC1', name: 'Монитор', room: '1' });
    await chooseType('t1');

    await clickSave();

    await vi.waitFor(() =>
      expect(notifications.error).toHaveBeenCalledWith(
        'Достигнут лимит: в базе не может быть больше 2000 записей',
      ),
    );
    expect(fixture.componentInstance.visible()).toBe(true);
  });

  it('save_deviceDeletedMeanwhile_showsNotFoundNotification', async () => {
    updateDevice.mockImplementation(() => throwError(() => conflict('Device.NotFound', 404)));
    await open(device);

    await clickSave();

    await vi.waitFor(() =>
      expect(notifications.error).toHaveBeenCalledWith(
        'Запись не найдена: возможно, её уже удалили',
      ),
    );
  });

  it('save_unexpectedProblem_showsServerDetail', async () => {
    updateDevice.mockImplementation(() =>
      throwError(() => conflict('Device.Invalid', 400, 'Device type does not exist.')),
    );
    await open(device);

    await clickSave();

    await vi.waitFor(() =>
      expect(notifications.error).toHaveBeenCalledWith('Device type does not exist.'),
    );
  });

  it('save_networkFailure_showsGenericError', async () => {
    updateDevice.mockImplementation(() =>
      throwError(() => new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') })),
    );
    await open(device);

    await clickSave();

    await vi.waitFor(() =>
      expect(notifications.error).toHaveBeenCalledWith('Не удалось сохранить запись'),
    );
  });

  it('cancel_click_closesWithoutSaving', async () => {
    await open(device);
    const cancel = [...document.querySelectorAll<HTMLElement>('.dx-popup-wrapper .dx-button')].find(
      (element) => element.textContent?.includes('Отмена'),
    )!;

    await userEvent.click(cancel);

    expect(fixture.componentInstance.visible()).toBe(false);
    expect(updateDevice).not.toHaveBeenCalled();
  });
});
