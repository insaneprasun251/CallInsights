export type CallStatus =
  | 'Queued'
  | 'Preprocessing'
  | 'Transcribing'
  | 'LabellingSpeakers'
  | 'BuildingPrompt'
  | 'Ready'
  | 'Failed'
  | 'GeneratingInsights';

export interface Recording {
  fileName: string;
  sizeBytes: number;
  modifiedUtc: string;
  callId: string | null;
  status: CallStatus | null; // null = not processed yet
  errorMessage: string | null;
}