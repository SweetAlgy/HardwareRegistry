import { request } from './http.js';

const API_URL = '/api/device-types';

export function getDeviceTypes() {
  return request('GET', API_URL);
}
