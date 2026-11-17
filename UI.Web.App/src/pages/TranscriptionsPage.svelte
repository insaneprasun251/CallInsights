<script lang="ts">
  import { onMount } from 'svelte';
  import FileJson from '@lucide/svelte/icons/file-json';
  import Download from '@lucide/svelte/icons/download';
  import RefreshCw from '@lucide/svelte/icons/refresh-cw';
  import Button from '../components/ui/Button.svelte';
  import Card from '../components/ui/Card.svelte';
  import Alert from '../components/ui/Alert.svelte';
  import EmptyState from '../components/ui/EmptyState.svelte';
  import Chip from '../components/ui/Chip.svelte';
  import { getTranscriptions, downloadTranscription } from '../services/transcriptionService';
  import type { Transcription } from '../types/transcription';

  let transcriptions: Transcription[] = $state([]);
  let loading = $state(true);
  let error = $state('');
  let downloadingCallId = $state('');

  async function loadTranscriptions() {
    loading = true;
    error = '';

    try {
      transcriptions = await getTranscriptions();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load transcriptions.';
    } finally {
      loading = false;
    }
  }

  async function download(callId: string) {
    downloadingCallId = callId;
    error = '';

    try {
      const response = await downloadTranscription(callId);
      if (!response.ok) {
        throw new Error(`Download failed (${response.status}).`);
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${callId}.json`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to download transcription.';
    } finally {
      downloadingCallId = '';
    }
  }

  function formatDuration(seconds: number | null) {
    if (seconds == null) return '—';
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    return `${minutes}:${String(remainingSeconds).padStart(2, '0')}`;
  }

  function formatBytes(bytes: number) {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  function formatDate(value: string) {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? '—'
      : date.toLocaleString();
  }

  onMount(loadTranscriptions);
</script>

<svelte:head>
  <title>Transcriptions | CallInsights</title>
</svelte:head>

<div class="space-y-8">
  <header class="flex flex-wrap items-start justify-between gap-4">
    <div>
      <h1 class="text-2xl font-semibold tracking-tight">Transcriptions</h1>
      <p class="mt-1 text-sm text-muted">
        Browse and download saved transcription JSON files.
      </p>
    </div>

    <Button
      variant="secondary"
      size="sm"
      disabled={loading}
      onclick={loadTranscriptions}
    >
      <RefreshCw size={15} strokeWidth={1.5} />
      {loading ? 'Refreshing…' : 'Refresh'}
    </Button>
  </header>

  {#if error}
    <Alert message={error} ondismiss={() => (error = '')} />
  {/if}

  <section>
    <div class="mb-3 flex items-baseline justify-between">
      <h2 class="text-sm font-semibold">Saved transcriptions</h2>
      {#if !loading}
        <span class="text-xs text-muted">
          {transcriptions.length} {transcriptions.length === 1 ? 'file' : 'files'}
        </span>
      {/if}
    </div>

    <Card class="overflow-hidden">
      {#if loading}
        <div class="divide-y divide-line" aria-busy="true">
          {#each [0, 1, 2] as i (i)}
            <div class="flex h-16 items-center gap-4 px-4">
              <div class="size-9 animate-pulse rounded-lg bg-canvas"></div>
              <div class="space-y-2">
                <div class="h-3 w-48 animate-pulse rounded bg-canvas"></div>
                <div class="h-3 w-28 animate-pulse rounded bg-canvas"></div>
              </div>
            </div>
          {/each}
        </div>
      {:else if transcriptions.length === 0}
        <EmptyState
          icon={FileJson}
          title="No transcriptions yet"
          description="Process a recording to generate a transcription JSON file."
        />
      {:else}
        <ul class="divide-y divide-line">
          {#each transcriptions as item (item.callId)}
            <li class="flex flex-col gap-4 px-4 py-4 sm:px-5">
              <div class="flex min-w-0 items-start gap-3">
                <div class="grid size-10 shrink-0 place-items-center rounded-lg bg-primary-tint text-primary-solid">
                  <FileJson size={20} strokeWidth={1.5} />
                </div>

                <div class="min-w-0 flex-1">
                  <p
                    class="truncate text-sm font-medium text-ink"
                    title={item.recordingFileName}
                  >
                    {item.recordingFileName || 'Recording name unavailable'}
                  </p>
                  <p class="mt-1 text-xs text-muted">
                    Modified {formatDate(item.modifiedUtc)}
                  </p>
                </div>

                <Button
                  size="sm"
                  variant="secondary"
                  disabled={downloadingCallId !== ''}
                  onclick={() => download(item.callId)}
                >
                  <Download size={15} strokeWidth={1.5} />
                  {downloadingCallId === item.callId ? 'Downloading…' : 'Download'}
                </Button>
              </div>

              <div class="flex flex-wrap items-center gap-2 pl-0 sm:pl-[52px]">
                <Chip>{item.language ?? 'Unknown language'}</Chip>
                <Chip>{formatDuration(item.durationSeconds)} duration</Chip>
                <Chip>{item.segmentCount} segments</Chip>
                <Chip>{formatBytes(item.sizeBytes)}</Chip>
              </div>
            </li>
          {/each}
        </ul>
      {/if}
    </Card>
  </section>
</div>
