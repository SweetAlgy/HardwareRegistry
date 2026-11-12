import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { Device, SaveDeviceDto } from '../models/device.model';
import { DevicesService } from './devices.service';

describe('DevicesService', () => {
  let service: DevicesService;
  let http: HttpTestingController;

  const dto: SaveDeviceDto = {
    inventoryNumber: 'PC1',
    name: 'Workstation',
    typeId: 't1',
    room: 101,
  };
  const device = { id: 'd1', ...dto, typeName: 'Ноутбук' } as Device;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(DevicesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getDevices_query_sendsPagingAndSortingParameters', async () => {
    const page = { items: [device], page: 2, pageSize: 20, totalCount: 21 };

    const result = firstValueFrom(
      service.getDevices({ page: 2, pageSize: 20, sortBy: 'room', sortOrder: 'descending' }),
    );

    const request = http.expectOne(
      (request) => request.url === '/api/devices' && request.method === 'GET',
    );
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('20');
    expect(request.request.params.get('sortBy')).toBe('room');
    expect(request.request.params.get('sortOrder')).toBe('descending');
    request.flush(page);

    expect(await result).toEqual(page);
  });

  it('createDevice_dto_postsItToCollection', async () => {
    const result = firstValueFrom(service.createDevice(dto));

    const request = http.expectOne({ url: '/api/devices', method: 'POST' });
    expect(request.request.body).toEqual(dto);
    request.flush(device);

    expect(await result).toEqual(device);
  });

  it('updateDevice_idAndDto_putsItToDeviceUrl', async () => {
    const result = firstValueFrom(service.updateDevice('d1', dto));

    const request = http.expectOne({ url: '/api/devices/d1', method: 'PUT' });
    expect(request.request.body).toEqual(dto);
    request.flush(device);

    expect(await result).toEqual(device);
  });

  it('deleteDevice_id_sendsDeleteToDeviceUrl', async () => {
    const result = firstValueFrom(service.deleteDevice('d1'), { defaultValue: undefined });

    http.expectOne({ url: '/api/devices/d1', method: 'DELETE' }).flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    await expect(result).resolves.toBeNull();
  });
});
