<script lang="ts">
  import FileAudio from '@lucide/svelte/icons/file-audio';
  import Play from '@lucide/svelte/icons/play';
  import RotateCw from '@lucide/svelte/icons/rotate-cw';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import Button from '../ui/Button.svelte';
  import Chip from '../ui/Chip.svelte';
  import StatusBadge from './StatusBadge.svelte';
  import { formatSize, formatDate } from '../../utils/format';
  import { canDelete } from '../../utils/status';
  import type { Recording } from '../../types/recording';

  interface Props {
    recording: Recording;
    selected: boolean;
    busy: boolean;
    onopen: () => void;
    onprocess: () => void;
    ondelete: () => void;
  }

  let { recording, selected, busy, onopen, onprocess, ondelete }: Props = $props();
</script>

<li class="relative flex h-11 items-center gap-4 px-4 hover:bg-canvas {selected ? 'bg-primary-tint/50' : ''}">
  <button
    type="button"
    class="flex h-full min-w-0 flex-1 items-center gap-4 text-left after:absolute after:inset-0 after:content-['']"
    onclick={onopen}
  >
    <FileAudio size={20} strokeWidth={1.5} class="shrink-0 text-muted" />
    <span class="truncate text-sm font-medium" title={recording.fileName}>{recording.fileName}</span>
  </button>

  <span class="hidden w-20 shrink-0 text-right text-xs text-muted sm:block">{formatSize(recording.sizeBytes)}</span>
  <Chip class="hidden shrink-0 md:inline-flex">{formatDate(recording.modifiedUtc)}</Chip>
  <div class="w-44 shrink-0"><StatusBadge status={recording.status} /></div>

  <div class="relative z-10 flex w-36 shrink-0 items-center justify-end gap-1">
    {#if recording.status === null}
      <Button size="sm" disabled={busy} onclick={onprocess}>
        <Play size={14} strokeWidth={1.5} />
        {busy ? 'Starting…' : 'Process'}
      </Button>
    {:else if recording.status === 'Failed'}
      <Button size="sm" variant="secondary" disabled={busy} onclick={onprocess}>
        <RotateCw size={14} strokeWidth={1.5} />
        {busy ? 'Starting…' : 'Retry'}
      </Button>
    {/if}

    {#if canDelete(recording.status)}
      <button
        type="button"
        class="grid size-8 place-items-center rounded-lg text-muted hover:bg-canvas hover:text-high-text"
        aria-label={`Delete ${recording.fileName}`}
        title="Delete"
        onclick={ondelete}
      >
        <Trash2 size={16} strokeWidth={1.5} />
      </button>
    {/if}
  </div>
</li>