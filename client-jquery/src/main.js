import $ from 'jquery';
// Must come before any widget import: registers the widgets as jQuery plugins.
import 'devextreme/integration/jquery';
import { loadMessages, locale } from 'devextreme/localization';
import ruMessages from 'devextreme/localization/messages/ru.json';
import 'devextreme/dist/css/dx.fluent.blue.light.compact.css';
import './styles.css';
import { createDeviceDialog } from './device-dialog.js';
import { createDevicesGrid } from './devices-grid.js';
import { connectDevicesHub } from './realtime/devices-hub.js';
import { debounce } from './utils/debounce.js';

loadMessages(ruMessages);
locale('ru');

$(() => {
  const deviceDialog = createDeviceDialog($('#device-dialog'));
  const devicesGrid = createDevicesGrid($('#devices-grid'), deviceDialog);

  deviceDialog.onSaved(() => devicesGrid.refresh());

  connectDevicesHub(debounce(() => devicesGrid.refresh(), REFRESH_DEBOUNCE_MS));
});

/** Bulk changes arrive as a burst of events; one reload covers them all. */
const REFRESH_DEBOUNCE_MS = 300;
