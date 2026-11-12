import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { DeviceTypesService } from './device-types.service';

describe('DeviceTypesService', () => {
  it('getDeviceTypes_dictionary_returnsServerList', async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    const types = [{ id: 't1', name: 'Монитор' }];

    const result = firstValueFrom(TestBed.inject(DeviceTypesService).getDeviceTypes());
    http.expectOne({ url: '/api/device-types', method: 'GET' }).flush(types);

    expect(await result).toEqual(types);
    http.verify();
  });
});
