import { apiRequest } from './apiClient';
import type { Transcription } from '../types/transcription';

export const getTranscriptions = () =>
  apiRequest<Transcription[]>('/transcriptions');

export const downloadTranscription = (callId: string) =>
  fetch(`/api/transcriptions/${encodeURIComponent(callId)}/download`);