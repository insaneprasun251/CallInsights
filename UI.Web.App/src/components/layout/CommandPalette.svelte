<script lang="ts">
  import { tick } from 'svelte';
  import Search from '@lucide/svelte/icons/search';
  import Upload from '@lucide/svelte/icons/upload';
  import PanelLeft from '@lucide/svelte/icons/panel-left';
  import FileAudio from '@lucide/svelte/icons/file-audio';
  import { routes } from '../../routes';
  import { router } from '../../router/router.svelte';
  import { ui } from '../../stores/ui.svelte';
  import { recordings } from '../../stores/recordings.svelte';
  import type { IconComponent } from '../../types/ui';

  interface Command {
    id: string;
    label: string;
    group: string;
    icon: IconComponent;
    run: () => void;
  }

  let query = $state('');
  let active = $state(0);
  let input = $state<HTMLInputElement>();

  const commands = $derived.by(() => {
    const list: Command[] = [];

    for (const r of routes.filter((r) => r.nav)) {
      list.push({ id: 'go-' + r.path, label: `Go to ${r.title}`, group: 'Pages', icon: r.icon, run: () => router.navigate(r.path) });
    }

    list.push({ id: 'upload', label: 'Upload recording', group: 'Actions', icon: Upload, run: () => recordings.chooseAndUpload() });
    list.push({
      id: 'sidebar',
      label: ui.sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar',
      group: 'Actions',
      icon: PanelLeft,
      run: () => ui.toggleSidebar()
    });

    for (const rec of recordings.items) {
      list.push({
        id: 'rec-' + rec.fileName,
        label: rec.fileName,
        group: 'Recordings',
        icon: FileAudio,
        run: () => {
          router.navigate('/');
          recordings.select(rec.fileName);
        }
      });
    }

    return list;
  });

  // recordings only show up once you start typing
  const results = $derived.by(() => {
    const q = query.trim().toLowerCase();
    const pool = q
      ? commands.filter((c) => c.label.toLowerCase().includes(q))
      : commands.filter((c) => c.group !== 'Recordings');
    return pool.slice(0, 12);
  });

  $effect(() => {
    if (ui.paletteOpen) {
      query = '';
      active = 0;
      tick().then(() => input?.focus());
    }
  });

  function execute(c: Command) {
    c.run();
    ui.paletteOpen = false;
  }

  function onKeydown(e: KeyboardEvent) {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      active = Math.min(active + 1, results.length - 1);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      active = Math.max(active - 1, 0);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const c = results[active];
      if (c) execute(c);
    } else if (e.key === 'Escape') {
      e.stopPropagation();
      ui.paletteOpen = false;
    }
  }
</script>

{#if ui.paletteOpen}
  <div class="fixed inset-0 z-50 flex items-start justify-center px-4 pt-[15vh]">
    <button
      type="button"
      class="absolute inset-0 cursor-default bg-ink/30"
      aria-label="Close command palette"
      tabindex="-1"
      onclick={() => (ui.paletteOpen = false)}
    ></button>

    <div
      role="dialog"
      aria-modal="true"
      aria-label="Command palette"
      class="relative w-full max-w-xl overflow-hidden rounded-lg border border-line bg-surface shadow-card"
    >
      <div class="flex h-14 items-center gap-3 border-b border-line px-4">
        <Search size={20} strokeWidth={1.5} class="shrink-0 text-muted" />
        <input
          bind:this={input}
          bind:value={query}
          oninput={() => (active = 0)}
          onkeydown={onKeydown}
          placeholder="Type a command or search recordings"
          aria-label="Command palette search"
          class="h-full flex-1 bg-transparent text-sm placeholder:text-muted focus:outline-none"
        />
      </div>

      <ul role="listbox" class="max-h-80 overflow-y-auto p-2">
        {#each results as c, i (c.id)}
          {#if i === 0 || results[i - 1].group !== c.group}
            <li role="presentation" class="px-3 pb-1 pt-3 text-xs font-medium text-muted first:pt-1">{c.group}</li>
          {/if}
          <li role="option" aria-selected={i === active}>
            <button
              type="button"
              class="flex h-10 w-full items-center gap-3 rounded-lg px-3 text-left text-sm {i === active ? 'bg-primary-tint' : ''}"
              onmousemove={() => (active = i)}
              onclick={() => execute(c)}
            >
              <c.icon size={20} strokeWidth={1.5} class="shrink-0 text-muted" />
              <span class="truncate">{c.label}</span>
            </button>
          </li>
        {:else}
          <li class="px-3 py-8 text-center text-sm text-muted">No results</li>
        {/each}
      </ul>

      <div class="border-t border-line px-4 py-2 text-xs text-muted">
        ↑ ↓ to move · Enter to select · Esc to close
      </div>
    </div>
  </div>
{/if}