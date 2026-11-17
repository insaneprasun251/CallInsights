import type { CallStatus } from '../types/recording';

interface StatusMeta {
  label: string;
  dot: string;
  pulse: boolean;
}

export const PIPELINE: CallStatus[] = [
  'Queued',
  'Preprocessing',
  'Transcribing',
  'LabellingSpeakers',
  'BuildingPrompt',
  'GeneratingInsights',
  'Ready'
];

const META: Record<CallStatus, StatusMeta> = {
  Queued: { label: 'Queued', dot: 'bg-medium', pulse: false },
  Preprocessing: { label: 'Preparing audio', dot: 'bg-low', pulse: true },
  Transcribing: { label: 'Transcribing', dot: 'bg-low', pulse: true },
  LabellingSpeakers: { label: 'Identifying speakers', dot: 'bg-low', pulse: true },
  BuildingPrompt: { label: 'Building prompt', dot: 'bg-low', pulse: true },
  GeneratingInsights: { label: 'Generating insights', dot: 'bg-low', pulse: true },
  Ready: { label: 'Ready', dot: 'bg-done', pulse: false },
  Failed: { label: 'Failed', dot: 'bg-high', pulse: false }
};

const NOT_PROCESSED: StatusMeta = {
  label: 'Not processed',
  dot: 'bg-idle',
  pulse: false
};

export const statusMeta = (status: CallStatus | null): StatusMeta =>
  status ? META[status] : NOT_PROCESSED;

export const isInProgress = (status: CallStatus | null) =>
  status === 'Preprocessing' ||
  status === 'Transcribing' ||
  status === 'LabellingSpeakers' ||
  status === 'BuildingPrompt' ||
  status === 'GeneratingInsights';

export const canDelete = (status: CallStatus | null) =>
  !isInProgress(status);