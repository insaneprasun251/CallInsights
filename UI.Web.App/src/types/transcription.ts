export interface Transcription {
  callId: string;
  recordingFileName: string;
  language: string | null;
  durationSeconds: number | null;
  segmentCount: number;
  modifiedUtc: string;
  sizeBytes: number;
}
