import { apiRequest } from './apiClient';
import type { Recording } from '../types/recording';

export const getRecordings = () => apiRequest<Recording[]>('/recordings');

export function uploadRecording(file: File) {
  const form = new FormData();
  form.append('file', file);
  return apiRequest<{ fileName: string }>('/recordings', { method: 'POST', body: form });
}

export const processRecording = (fileName: string) =>
  apiRequest<{ callId: string }>(`/recordings/${encodeURIComponent(fileName)}/process`, {
    method: 'POST'
  });

export const deleteRecording = (fileName: string) =>
  apiRequest<void>(`/recordings/${encodeURIComponent(fileName)}`, { method: 'DELETE' });