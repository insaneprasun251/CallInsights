<script lang="ts">
  import Search from '@lucide/svelte/icons/search';
  import Upload from '@lucide/svelte/icons/upload';
  import Button from '../ui/Button.svelte';
  import { recordings } from '../../stores/recordings.svelte';
  import { ui } from '../../stores/ui.svelte';
</script>

<header class="flex h-16 shrink-0 items-center gap-4 border-b border-line px-8">
  <div class="relative max-w-md flex-1">
    <Search size={16} strokeWidth={1.5} class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
    <input
      type="search"
      aria-label="Search recordings"
      placeholder="Search recordings"
      bind:value={recordings.query}
      class="h-10 w-full rounded-lg border border-line bg-surface pl-10 pr-16 text-sm placeholder:text-muted"
    />
    <button
      type="button"
      class="absolute right-2 top-1/2 -translate-y-1/2 rounded-md border border-line bg-canvas px-1.5 py-0.5 text-xs text-muted hover:text-ink"
      aria-label="Open command palette"
      onclick={() => (ui.paletteOpen = true)}
    >
      Ctrl K
    </button>
  </div>

  <div class="ml-auto">
    <Button onclick={() => recordings.chooseAndUpload()}>
      <Upload size={16} strokeWidth={1.5} />
      Upload
    </Button>
  </div>
</header>