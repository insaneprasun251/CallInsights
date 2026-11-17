<script lang="ts">
  import { tick } from 'svelte';
  import Button from './Button.svelte';

  interface Props {
    open: boolean;
    title: string;
    message: string;
    confirmLabel?: string;
    busy?: boolean;
    onconfirm: () => void;
    oncancel: () => void;
  }

  let { open, title, message, confirmLabel = 'Delete', busy = false, onconfirm, oncancel }: Props = $props();

  let panel = $state<HTMLDivElement>();

  $effect(() => {
    if (open) tick().then(() => panel?.focus());
  });
</script>

<svelte:window onkeydown={(e) => open && e.key === 'Escape' && oncancel()} />

{#if open}
  <div class="fixed inset-0 z-50 grid place-items-center px-4">
    <button
      type="button"
      class="absolute inset-0 cursor-default bg-ink/30"
      aria-label="Cancel"
      tabindex="-1"
      onclick={oncancel}
    ></button>

    <div
      bind:this={panel}
      role="alertdialog"
      aria-modal="true"
      aria-label={title}
      tabindex="-1"
      class="relative w-full max-w-md rounded-lg border border-line bg-surface p-6 shadow-card focus:outline-none"
    >
      <h2 class="text-base font-semibold">{title}</h2>
      <p class="mt-2 text-sm text-muted">{message}</p>
      <div class="mt-6 flex justify-end gap-3">
        <Button variant="secondary" disabled={busy} onclick={oncancel}>Cancel</Button>
        <Button variant="danger" disabled={busy} onclick={onconfirm}>
          {busy ? 'Deleting…' : confirmLabel}
        </Button>
      </div>
    </div>
  </div>
{/if}