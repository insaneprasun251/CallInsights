<script lang="ts">
  import CircleAlert from '@lucide/svelte/icons/circle-alert';
  import X from '@lucide/svelte/icons/x';

  let { message, ondismiss }: { message: string; ondismiss: () => void } = $props();
</script>

<div role="alert" class="flex items-start gap-3 rounded-lg border border-high/30 bg-high/10 px-4 py-3 text-sm text-ink">
  <CircleAlert size={20} strokeWidth={1.5} class="mt-px shrink-0 text-high-text" />
  <p class="flex-1">{message}</p>
  <button
    type="button"
    class="shrink-0 rounded-md p-0.5 text-muted hover:text-ink"
    aria-label="Dismiss"
    onclick={ondismiss}
  >
    <X size={16} strokeWidth={1.5} />
  </button>
</div>