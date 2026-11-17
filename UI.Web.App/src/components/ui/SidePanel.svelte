<script lang="ts">
  import type { Snippet } from 'svelte';
  import { fly, fade } from 'svelte/transition';
  import X from '@lucide/svelte/icons/x';

  let {
    open,
    title,
    onclose,
    children
  }: { open: boolean; title: string; onclose: () => void; children: Snippet } = $props();
</script>

<svelte:window onkeydown={(e) => open && e.key === 'Escape' && onclose()} />

{#if open}
  <button
    type="button"
    class="fixed inset-0 z-30 cursor-default bg-ink/10"
    aria-label="Close panel"
    tabindex="-1"
    transition:fade={{ duration: 150 }}
    onclick={onclose}
  ></button>
  <aside
    class="fixed inset-y-0 right-0 z-40 flex w-full max-w-md flex-col border-l border-line bg-surface shadow-card"
    aria-label={title}
    transition:fly={{ x: 48, duration: 150 }}
  >
    <header class="flex h-16 shrink-0 items-center justify-between border-b border-line px-6">
      <h2 class="text-sm font-semibold">{title}</h2>
      <button
        type="button"
        class="rounded-lg p-2 text-muted hover:bg-canvas hover:text-ink"
        aria-label="Close"
        onclick={onclose}
      >
        <X size={20} strokeWidth={1.5} />
      </button>
    </header>
    <div class="flex-1 overflow-y-auto p-6">
      {@render children()}
    </div>
  </aside>
{/if}