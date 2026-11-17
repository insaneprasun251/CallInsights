<script lang="ts">
  import Check from '@lucide/svelte/icons/check';
  import Play from '@lucide/svelte/icons/play';
  import RotateCw from '@lucide/svelte/icons/rotate-cw';
  import Button from '../ui/Button.svelte';
  import StatusBadge from './StatusBadge.svelte';
  import { PIPELINE, statusMeta } from '../../utils/status';
  import { formatSize, formatDate } from '../../utils/format';
  import { recordings } from '../../stores/recordings.svelte';
  import type { Recording } from '../../types/recording';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import { canDelete } from '../../utils/status';

  let { recording }: { recording: Recording } = $props();

  const busy = $derived(recordings.busyFiles.includes(recording.fileName));
  const canProcess = $derived(recording.status === null || recording.status === 'Failed');

  function phase(i: number): 'done' | 'current' | 'pending' {
    const s = recording.status;
    if (s === null || s === 'Failed') return 'pending';
    const current = PIPELINE.indexOf(s);
    if (s === 'Ready' || i < current) return 'done';
    return i === current ? 'current' : 'pending';
  }
</script>

<div class="space-y-8">
  <div>
    <p class="break-words text-base font-semibold">{recording.fileName}</p>
    <div class="mt-2"><StatusBadge status={recording.status} /></div>
  </div>

  {#if recording.status === 'Failed' && recording.errorMessage}
    <p class="rounded-lg border border-high/30 bg-high/10 px-4 py-3 text-sm text-ink">
      <span class="font-medium text-high-text">Processing failed.</span>
      {recording.errorMessage}
    </p>
  {/if}

  <dl class="grid grid-cols-2 gap-4 text-sm">
    <div>
      <dt class="text-muted">Size</dt>
      <dd class="mt-1 font-medium">{formatSize(recording.sizeBytes)}</dd>
    </div>
    <div>
      <dt class="text-muted">Added</dt>
      <dd class="mt-1 font-medium">{formatDate(recording.modifiedUtc)}</dd>
    </div>
  </dl>

  <section>
    <h3 class="mb-4 text-sm font-semibold">Progress</h3>
    <ol>
      {#each PIPELINE as stage, i (stage)}
        {@const p = phase(i)}
        <li class="flex gap-4">
          <div class="flex flex-col items-center">
            <span
              class="grid size-6 place-items-center rounded-full border text-xs
                {p === 'done' ? 'border-sage-text bg-sage-text text-white' : ''}
                {p === 'current' ? 'border-primary-solid bg-primary-tint text-primary-solid' : ''}
                {p === 'pending' ? 'border-line bg-surface text-muted' : ''}"
            >
              {#if p === 'done'}<Check size={14} strokeWidth={1.5} />{:else}{i + 1}{/if}
            </span>
            {#if i < PIPELINE.length - 1}<span class="h-6 w-px bg-line"></span>{/if}
          </div>
          <span class="pt-0.5 text-sm {p === 'pending' ? 'text-muted' : 'text-ink'} {p === 'current' ? 'font-medium' : ''}">
            {statusMeta(stage).label}
          </span>
        </li>
      {/each}
    </ol>
  </section>

  {#if canProcess}
    <Button
      class="w-full"
      variant={recording.status === 'Failed' ? 'secondary' : 'primary'}
      disabled={busy}
      onclick={() => recordings.process(recording.fileName)}
    >
      {#if recording.status === 'Failed'}
        <RotateCw size={16} strokeWidth={1.5} />
        {busy ? 'Starting…' : 'Retry processing'}
      {:else}
        <Play size={16} strokeWidth={1.5} />
        {busy ? 'Starting…' : 'Process recording'}
      {/if}
    </Button>
  {/if}

  {#if canDelete(recording.status)}
    <Button class="w-full" variant="ghost" onclick={() => recordings.requestDelete(recording.fileName)}>
      <Trash2 size={16} strokeWidth={1.5} />
      Delete recording
    </Button>
  {/if}
</div>