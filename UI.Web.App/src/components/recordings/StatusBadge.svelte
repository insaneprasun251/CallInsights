<script lang="ts">
  import CircleCheck from '@lucide/svelte/icons/circle-check';
  import { statusMeta } from '../../utils/status';
  import type { CallStatus } from '../../types/recording';

  let { status }: { status: CallStatus | null } = $props();

  const meta = $derived(statusMeta(status));
</script>

<span class="inline-flex items-center gap-2 text-sm text-ink">
  {#if status === 'Ready'}
    <CircleCheck size={16} strokeWidth={1.5} class="animate-pop text-sage-text" />
  {:else}
    <span class="size-2 rounded-full {meta.dot} {meta.pulse ? 'animate-pulse' : ''}"></span>
  {/if}
  {meta.label}
</span>