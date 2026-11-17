<script lang="ts">
  import type { Snippet } from 'svelte';
  import Sidebar from './Sidebar.svelte';
  import Topbar from './Topbar.svelte';
  import CommandPalette from './CommandPalette.svelte';
  import { ui } from '../../stores/ui.svelte';

  let { children }: { children: Snippet } = $props();

  function onKeydown(e: KeyboardEvent) {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault();
      ui.paletteOpen = !ui.paletteOpen;
    }
  }
</script>

<svelte:window onkeydown={onKeydown} />

<div class="flex h-full">
  <Sidebar />
  <div class="flex min-w-0 flex-1 flex-col">
    <Topbar />
    <main class="flex-1 overflow-y-auto">
      <div class="mx-auto w-full max-w-5xl px-8 py-8">
        {@render children()}
      </div>
    </main>
  </div>
</div>

<CommandPalette />