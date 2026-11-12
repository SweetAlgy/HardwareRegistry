import 'devextreme/integration/jquery';
import 'devextreme/dist/css/dx.fluent.blue.light.compact.css';
import $ from 'jquery';
import { afterEach } from 'vitest';

afterEach(() => {
  // Widgets and their dialogs live in <body>; leave nothing behind for the next test.
  $('.dx-overlay-wrapper').remove();
  document.body.innerHTML = '';
});
