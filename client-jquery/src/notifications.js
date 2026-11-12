import notify from 'devextreme/ui/notify';

const DISPLAY_TIME = 4000;

function show(message, type) {
  notify(
    { message, type, displayTime: DISPLAY_TIME },
    { position: 'bottom right', direction: 'up-push' },
  );
}

export function notifySuccess(message) {
  show(message, 'success');
}

export function notifyError(message) {
  show(message, 'error');
}
