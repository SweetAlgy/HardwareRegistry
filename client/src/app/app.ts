import { Component } from '@angular/core';
import { DevicesTable } from './features/devices/components/devices-table/devices-table';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
  imports: [DevicesTable],
})
export class App {}
