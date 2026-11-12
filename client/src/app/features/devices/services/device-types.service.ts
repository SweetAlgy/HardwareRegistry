import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DeviceType } from '../models/device-type.model';

@Injectable({
  providedIn: 'root',
})
export class DeviceTypesService {
  private readonly _apiUrl = '/api/device-types';

  public constructor(private _http: HttpClient) {}

  public getDeviceTypes(): Observable<DeviceType[]> {
    return this._http.get<DeviceType[]>(this._apiUrl);
  }
}
