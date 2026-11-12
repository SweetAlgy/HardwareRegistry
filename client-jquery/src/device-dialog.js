import CustomStore from 'devextreme/data/custom_store';
import 'devextreme/ui/button';
import 'devextreme/ui/form';
import 'devextreme/ui/number_box';
import 'devextreme/ui/popup';
import 'devextreme/ui/select_box';
import 'devextreme/ui/text_box';
import $ from 'jquery';
import { getDeviceTypes } from './api/device-types-api.js';
import { createDevice, updateDevice } from './api/devices-api.js';
import { toProblemDetails } from './api/http.js';
import { DEVICE_CONSTRAINTS } from './constraints.js';
import { notifyError, notifySuccess } from './notifications.js';

/** @param {JQuery} $container */
export function createDeviceDialog($container) {
  const savedCallbacks = $.Callbacks();

  /** The device being edited; `null` when the dialog is open for a new one. */
  let editedDevice = null;

  /** Inventory number the server rejected as a duplicate; fails validation until changed. */
  let takenInventoryNumber = null;

  let isSaving = false;

  const $form = $('<div>');

  const popup = $container
    .dxPopup({
      width: 480,
      height: 'auto',
      showCloseButton: true,
      hideOnOutsideClick: false,
      // The form must exist before the first open() fills it.
      deferRendering: false,
      contentTemplate: () => $form,
      toolbarItems: [
        {
          widget: 'dxButton',
          toolbar: 'bottom',
          location: 'after',
          options: {
            text: 'Сохранить',
            type: 'default',
            stylingMode: 'contained',
            onClick: save,
          },
        },
        {
          widget: 'dxButton',
          toolbar: 'bottom',
          location: 'after',
          options: {
            text: 'Отмена',
            stylingMode: 'outlined',
            onClick: () => popup.hide(),
          },
        },
      ],
      onShown: () => form.getEditor('inventoryNumber')?.focus(),
    })
    .dxPopup('instance');

  const form = $form
    .dxForm({
      formData: toFormData(null),
      labelLocation: 'top',
      showColonAfterLabel: false,
      onEditorEnterKey: save,
      items: [
        {
          dataField: 'inventoryNumber',
          editorType: 'dxTextBox',
          label: { text: 'Учетный номер' },
          editorOptions: {
            maxLength: DEVICE_CONSTRAINTS.inventoryNumberMaxLength,
            validationMessageMode: 'always',
          },
          validationRules: [
            { type: 'required', message: 'Укажите учетный номер' },
            {
              type: 'pattern',
              pattern: DEVICE_CONSTRAINTS.inventoryNumberPattern,
              message: 'Только цифры и латинские буквы',
            },
            {
              type: 'stringLength',
              max: DEVICE_CONSTRAINTS.inventoryNumberMaxLength,
              message: `Не длиннее ${DEVICE_CONSTRAINTS.inventoryNumberMaxLength} символов`,
            },
            {
              type: 'custom',
              reevaluate: true,
              validationCallback: (options) => options.value !== takenInventoryNumber,
              message: 'Техника с таким учетным номером уже существует',
            },
          ],
        },
        {
          dataField: 'name',
          editorType: 'dxTextBox',
          label: { text: 'Наименование' },
          editorOptions: {
            maxLength: DEVICE_CONSTRAINTS.nameMaxLength,
            validationMessageMode: 'always',
          },
          validationRules: [
            { type: 'required', message: 'Укажите наименование' },
            {
              type: 'stringLength',
              max: DEVICE_CONSTRAINTS.nameMaxLength,
              message: `Не длиннее ${DEVICE_CONSTRAINTS.nameMaxLength} символов`,
            },
          ],
        },
        {
          dataField: 'typeId',
          editorType: 'dxSelectBox',
          label: { text: 'Тип техники' },
          editorOptions: {
            // Raw mode loads the dictionary once and keeps it in memory.
            dataSource: new CustomStore({ key: 'id', loadMode: 'raw', load: getDeviceTypes }),
            valueExpr: 'id',
            displayExpr: 'name',
            placeholder: 'Выберите тип',
            searchEnabled: true,
            validationMessageMode: 'always',
          },
          validationRules: [{ type: 'required', message: 'Выберите тип техники' }],
        },
        {
          dataField: 'room',
          editorType: 'dxNumberBox',
          label: { text: 'Комната размещения' },
          editorOptions: {
            min: DEVICE_CONSTRAINTS.roomMin,
            max: DEVICE_CONSTRAINTS.roomMax,
            format: '#',
            showSpinButtons: true,
            validationMessageMode: 'always',
          },
          validationRules: [
            { type: 'required', message: 'Укажите комнату' },
            {
              type: 'range',
              min: DEVICE_CONSTRAINTS.roomMin,
              max: DEVICE_CONSTRAINTS.roomMax,
              message: `Целое число от ${DEVICE_CONSTRAINTS.roomMin} до ${DEVICE_CONSTRAINTS.roomMax}`,
            },
          ],
        },
      ],
    })
    .dxForm('instance');

  function open(device) {
    editedDevice = device;
    takenInventoryNumber = null;

    form.option('formData', toFormData(device));
    popup.option('title', device ? 'Редактирование техники' : 'Новая техника');
    popup.show();
  }

  async function save() {
    if (isSaving) return;
    if (!form.validate().isValid) return;

    const formData = form.option('formData');
    const dto = {
      inventoryNumber: formData.inventoryNumber,
      name: formData.name.trim(),
      typeId: formData.typeId,
      room: formData.room,
    };

    isSaving = true;

    try {
      const savedDevice = editedDevice
        ? await updateDevice(editedDevice.id, dto)
        : await createDevice(dto);

      notifySuccess(
        editedDevice
          ? `Техника «${savedDevice.inventoryNumber}» сохранена`
          : `Техника «${savedDevice.inventoryNumber}» добавлена`,
      );

      popup.hide();
      savedCallbacks.fire(savedDevice);
    } catch (error) {
      handleError(error, dto);
    } finally {
      isSaving = false;
    }
  }

  function handleError(error, dto) {
    const problem = toProblemDetails(error);

    switch (problem?.code) {
      case 'Device.AlreadyExists':
        takenInventoryNumber = dto.inventoryNumber;
        form.validate();
        break;
      case 'Operation.LimitExceeded':
        notifyError(
          `Достигнут лимит: в базе не может быть больше ${DEVICE_CONSTRAINTS.maxCount} записей`,
        );
        break;
      case 'Device.NotFound':
        notifyError('Запись не найдена: возможно, её уже удалили');
        break;
      default:
        notifyError(problem?.detail ?? 'Не удалось сохранить запись');
    }
  }

  return {
    open,
    onSaved: (callback) => savedCallbacks.add(callback),
  };
}

function toFormData(device) {
  return {
    inventoryNumber: device?.inventoryNumber ?? '',
    name: device?.name ?? '',
    typeId: device?.typeId ?? null,
    room: device?.room ?? null,
  };
}
