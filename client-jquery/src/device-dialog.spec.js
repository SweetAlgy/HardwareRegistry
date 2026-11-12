import $ from 'jquery';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { userEvent } from 'vitest/browser';
import { getDeviceTypes } from './api/device-types-api.js';
import { createDevice, updateDevice } from './api/devices-api.js';
import { createDeviceDialog } from './device-dialog.js';
import { notifyError, notifySuccess } from './notifications.js';

vi.mock('./api/devices-api.js', () => ({ createDevice: vi.fn(), updateDevice: vi.fn() }));
vi.mock('./api/device-types-api.js', () => ({ getDeviceTypes: vi.fn() }));
vi.mock('./notifications.js', () => ({ notifySuccess: vi.fn(), notifyError: vi.fn() }));

const types = [
  { id: 't1', name: 'Монитор' },
  { id: 't2', name: 'Ноутбук' },
];

const device = {
  id: 'd1',
  inventoryNumber: 'MON7',
  name: 'Dell P2422H',
  typeId: 't1',
  typeName: 'Монитор',
  room: 305,
};

/** A rejected `$.ajax` the way the API answers with ProblemDetails. */
function problem(code, status, detail) {
  return Object.assign(new Error(code), {
    status,
    responseJSON: { status, title: 'Error', code, detail },
  });
}

describe('createDeviceDialog', () => {
  let dialog;
  let $container;
  let saved;

  function form() {
    return $('.dx-popup-wrapper .dx-form').dxForm('instance');
  }

  function popup() {
    return $container.dxPopup('instance');
  }

  /** The input the user types into; some editors keep their `name` on a hidden one. */
  function input(dataField) {
    return form().getEditor(dataField).element().find('input.dx-texteditor-input')[0];
  }

  function popupText() {
    return $('.dx-popup-wrapper').text();
  }

  async function open(edited) {
    dialog.open(edited);
    await vi.waitFor(() => expect($('.dx-popup-wrapper .dx-form').length).toBe(1));
    await vi.waitFor(() => expect(popup().option('visible')).toBe(true));
  }

  async function fill(card) {
    for (const [field, value] of Object.entries(card)) {
      await userEvent.fill(input(field), value);
    }
  }

  async function clickButton(text) {
    const button = $('.dx-popup-wrapper .dx-button')
      .filter((_, element) => $(element).text().includes(text))
      .get(0);

    await userEvent.click(button);
  }

  async function fillValidCard() {
    await fill({ inventoryNumber: 'PC1', name: '  Рабочая станция  ', room: '101' });
    form().updateData('typeId', 't2');
  }

  beforeEach(() => {
    vi.clearAllMocks();
    getDeviceTypes.mockResolvedValue(types);
    createDevice.mockImplementation(async (dto) => ({ ...device, ...dto, id: 'new' }));
    updateDevice.mockImplementation(async (id, dto) => ({ ...device, ...dto, id }));

    $container = $('<div>').appendTo(document.body);
    dialog = createDeviceDialog($container);
    saved = vi.fn();
    dialog.onSaved(saved);
  });

  it('open_newDevice_showsEmptyCardTitledNew', async () => {
    await open(null);

    expect(popup().option('title')).toBe('Новая техника');
    expect(input('inventoryNumber').value).toBe('');
    for (const label of ['Учетный номер', 'Наименование', 'Тип техники', 'Комната размещения']) {
      expect(popupText()).toContain(label);
    }
  });

  it('open_existingDevice_fillsCardWithItsValues', async () => {
    await open(device);

    expect(popup().option('title')).toBe('Редактирование техники');
    expect(input('inventoryNumber').value).toBe('MON7');
    expect(input('name').value).toBe('Dell P2422H');
    expect(form().option('formData')).toMatchObject({ typeId: 't1', room: 305 });
  });

  it('open_afterEditing_resetsCardForNewDevice', async () => {
    await open(device);
    popup().hide();

    await open(null);

    expect(input('inventoryNumber').value).toBe('');
  });

  it('save_emptyCard_showsRequiredMessagesWithoutCallingApi', async () => {
    await open(null);

    await clickButton('Сохранить');

    await vi.waitFor(() => expect(popupText()).toContain('Укажите учетный номер'));
    expect(popupText()).toContain('Укажите наименование');
    expect(popupText()).toContain('Выберите тип техники');
    expect(popupText()).toContain('Укажите комнату');
    expect(createDevice).not.toHaveBeenCalled();
  });

  it.each(['ПК1', 'PC-1', 'PC 1'])('save_inventoryNumber%j_showsFormatMessage', async (value) => {
    await open(null);
    await fillValidCard();
    await fill({ inventoryNumber: value });

    await clickButton('Сохранить');

    await vi.waitFor(() => expect(popupText()).toContain('Только цифры и латинские буквы'));
    expect(createDevice).not.toHaveBeenCalled();
  });

  it('save_roomOfZero_showsRangeMessage', async () => {
    await open(null);
    await fillValidCard();
    form().updateData('room', 0);

    await clickButton('Сохранить');

    await vi.waitFor(() => expect(popupText()).toContain('Целое число от 1 до 1000'));
    expect(createDevice).not.toHaveBeenCalled();
  });

  it.each(['0', '1001', '5000', '-3'])(
    'save_typedRoom%s_neverSendsRoomOutsideRange',
    async (room) => {
      await open(null);
      await fillValidCard();
      await fill({ room });
      await userEvent.tab();

      await clickButton('Сохранить');
      await new Promise((resolve) => setTimeout(resolve, 200));

      for (const [dto] of createDevice.mock.calls) {
        expect(dto.room).toBeGreaterThanOrEqual(1);
        expect(dto.room).toBeLessThanOrEqual(1000);
      }
    },
  );

  it('editors_lengthLimits_matchSpecification', async () => {
    await open(null);

    expect(input('inventoryNumber').maxLength).toBe(32);
    expect(input('name').maxLength).toBe(256);
  });

  it('typeEditor_dictionary_offersEveryType', async () => {
    await open(null);

    const items = await form().getEditor('typeId').getDataSource().load();

    expect(items).toEqual(types);
  });

  it('save_validNewCard_createsDeviceWithTrimmedName', async () => {
    await open(null);
    await fillValidCard();

    await clickButton('Сохранить');

    await vi.waitFor(() =>
      expect(createDevice).toHaveBeenCalledWith({
        inventoryNumber: 'PC1',
        name: 'Рабочая станция',
        typeId: 't2',
        room: 101,
      }),
    );
  });

  it('save_validNewCard_notifiesFiresSavedAndCloses', async () => {
    await open(null);
    await fillValidCard();

    await clickButton('Сохранить');

    await vi.waitFor(() => expect(saved).toHaveBeenCalledOnce());
    expect(notifySuccess).toHaveBeenCalledWith('Техника «PC1» добавлена');
    await vi.waitFor(() => expect(popup().option('visible')).toBe(false));
  });

  it('save_enterKeyInEditor_submits', async () => {
    await open(null);
    await fillValidCard();

    await userEvent.type(input('name'), '{Enter}');

    await vi.waitFor(() => expect(createDevice).toHaveBeenCalledOnce());
  });

  it('save_editedDevice_updatesIt', async () => {
    await open(device);
    await fill({ name: 'Dell U2723QE', room: '306' });

    await clickButton('Сохранить');

    await vi.waitFor(() =>
      expect(updateDevice).toHaveBeenCalledWith('d1', {
        inventoryNumber: 'MON7',
        name: 'Dell U2723QE',
        typeId: 't1',
        room: 306,
      }),
    );
    expect(createDevice).not.toHaveBeenCalled();
    expect(notifySuccess).toHaveBeenCalledWith('Техника «MON7» сохранена');
  });

  it('save_doubleClickOnSave_sendsOneRequest', async () => {
    let resolveCreate;
    createDevice.mockImplementation(() => new Promise((resolve) => (resolveCreate = resolve)));
    await open(null);
    await fillValidCard();

    await clickButton('Сохранить');
    await clickButton('Сохранить');
    resolveCreate({ ...device, inventoryNumber: 'PC1' });

    await vi.waitFor(() => expect(saved).toHaveBeenCalledOnce());
    expect(createDevice).toHaveBeenCalledOnce();
  });

  it('save_duplicateInventoryNumber_marksFieldUntilChanged', async () => {
    createDevice.mockRejectedValue(problem('Device.AlreadyExists', 409));
    await open(null);
    await fillValidCard();

    await clickButton('Сохранить');

    await vi.waitFor(() =>
      expect(popupText()).toContain('Техника с таким учетным номером уже существует'),
    );
    expect(popup().option('visible')).toBe(true);
    expect(saved).not.toHaveBeenCalled();

    await fill({ inventoryNumber: 'PC2' });
    await userEvent.tab();

    await vi.waitFor(() =>
      expect(popupText()).not.toContain('Техника с таким учетным номером уже существует'),
    );
  });

  it('save_limitExceeded_showsLimitNotificationAndStaysOpen', async () => {
    createDevice.mockRejectedValue(problem('Operation.LimitExceeded', 400));
    await open(null);
    await fillValidCard();

    await clickButton('Сохранить');

    await vi.waitFor(() =>
      expect(notifyError).toHaveBeenCalledWith(
        'Достигнут лимит: в базе не может быть больше 2000 записей',
      ),
    );
    expect(popup().option('visible')).toBe(true);
  });

  it('save_deviceDeletedMeanwhile_showsNotFoundNotification', async () => {
    updateDevice.mockRejectedValue(problem('Device.NotFound', 404));
    await open(device);

    await clickButton('Сохранить');

    await vi.waitFor(() =>
      expect(notifyError).toHaveBeenCalledWith('Запись не найдена: возможно, её уже удалили'),
    );
  });

  it('save_unexpectedProblem_showsServerDetail', async () => {
    updateDevice.mockRejectedValue(problem('Device.Invalid', 400, 'Device type does not exist.'));
    await open(device);

    await clickButton('Сохранить');

    await vi.waitFor(() => expect(notifyError).toHaveBeenCalledWith('Device type does not exist.'));
  });

  it('save_networkFailure_showsGenericError', async () => {
    updateDevice.mockRejectedValue(Object.assign(new Error('offline'), { status: 0 }));
    await open(device);

    await clickButton('Сохранить');

    await vi.waitFor(() => expect(notifyError).toHaveBeenCalledWith('Не удалось сохранить запись'));
  });

  it('cancel_click_closesWithoutSaving', async () => {
    await open(device);

    await clickButton('Отмена');

    await vi.waitFor(() => expect(popup().option('visible')).toBe(false));
    expect(updateDevice).not.toHaveBeenCalled();
  });
});
