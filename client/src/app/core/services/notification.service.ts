import { Injectable } from '@angular/core';
import notify from 'devextreme/ui/notify';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly _displayTime = 4000;

  public success(message: string): void {
    this.show(message, 'success');
  }

  public error(message: string): void {
    this.show(message, 'error');
  }

  private show(message: string, type: 'success' | 'error'): void {
    notify(
      { message, type, displayTime: this._displayTime },
      { position: 'bottom right', direction: 'up-push' },
    );
  }
}
