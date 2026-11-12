import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../../../core/models/paged-result.model';
import { Device, DevicesQuery, SaveDeviceDto } from '../models/device.model';

@Injectable({
  providedIn: 'root',
})
export class DevicesService {
  private readonly _apiUrl = '/api/devices';

  public constructor(private _http: HttpClient) {}

  public getDevices(query: DevicesQuery): Observable<PagedResult<Device>> {
    const params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortOrder', query.sortOrder);

    return this._http.get<PagedResult<Device>>(this._apiUrl, { params });
  }

  public createDevice(dto: SaveDeviceDto): Observable<Device> {
    return this._http.post<Device>(this._apiUrl, dto);
  }

  public updateDevice(deviceId: string, dto: SaveDeviceDto): Observable<Device> {
    return this._http.put<Device>(`${this._apiUrl}/${deviceId}`, dto);
  }

  public deleteDevice(deviceId: string): Observable<void> {
    return this._http.delete<void>(`${this._apiUrl}/${deviceId}`);
  }
}
