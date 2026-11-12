import $ from 'jquery';
import { request } from './http.js';

const API_URL = '/api/devices';

/** @param {{ page: number, pageSize: number, sortBy: string, sortOrder: string }} query */
export function getDevices(query) {
  return request('GET', `${API_URL}?${$.param(query)}`);
}

export function createDevice(dto) {
  return request('POST', API_URL, dto);
}

export function updateDevice(deviceId, dto) {
  return request('PUT', `${API_URL}/${deviceId}`, dto);
}

export function deleteDevice(deviceId) {
  return request('DELETE', `${API_URL}/${deviceId}`);
}
