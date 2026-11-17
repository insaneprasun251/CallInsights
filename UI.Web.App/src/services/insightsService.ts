import { apiRequest } from './apiClient';
import type { CallInsight } from '../types/insight';

export const getInsights = (callId: string) =>
  apiRequest<CallInsight[]>(`/insights/${encodeURIComponent(callId)}`);