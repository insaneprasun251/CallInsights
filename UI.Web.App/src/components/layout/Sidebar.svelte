<script lang="ts">
  import AudioLines from '@lucide/svelte/icons/audio-lines';
  import PanelLeftClose from '@lucide/svelte/icons/panel-left-close';
  import PanelLeftOpen from '@lucide/svelte/icons/panel-left-open';
  import { routes } from '../../routes';
  import { router } from '../../router/router.svelte';
  import { ui } from '../../stores/ui.svelte';

  const navRoutes = routes.filter((r) => r.nav);
  const collapsed = $derived(ui.sidebarCollapsed);
</script>

<aside
  class="flex shrink-0 flex-col bg-sidebar transition-[width] duration-150 {collapsed ? 'w-16' : 'w-64'}"
  aria-label="Sidebar"
>
  <div class="flex h-16 items-center gap-3 px-4">
    <span class="grid size-8 shrink-0 place-items-center rounded-lg bg-primary-solid text-white">
      <AudioLines size={18} strokeWidth={1.5} />
    </span>
    {#if !collapsed}
      <span class="truncate text-sm font-semibold">Call Insights</span>
    {/if}
  </div>

  <nav class="flex-1 space-y-1 px-2 py-2" aria-label="Main">
    {#each navRoutes as r (r.path)}
      {@const active = router.path === r.path}
      <a
        href={'#' + r.path}
        title={r.title}
        aria-current={active ? 'page' : undefined}
        class="flex h-10 items-center gap-3 rounded-lg text-sm font-medium {collapsed ? 'justify-center' : 'px-3'}
          {active ? 'bg-surface text-ink shadow-card' : 'text-muted-sidebar hover:bg-surface/60 hover:text-ink'}"
      >
        <r.icon size={20} strokeWidth={1.5} class="shrink-0 {active ? 'text-primary-solid' : ''}" />
        {#if !collapsed}<span class="truncate">{r.title}</span>{/if}
      </a>
    {/each}
  </nav>

  <div class="p-2">
    <button
      type="button"
      class="flex h-10 w-full items-center gap-3 rounded-lg text-sm text-muted-sidebar hover:bg-surface/60 hover:text-ink {collapsed ? 'justify-center' : 'px-3'}"
      aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
      onclick={() => ui.toggleSidebar()}
    >
      {#if collapsed}
        <PanelLeftOpen size={20} strokeWidth={1.5} />
      {:else}
        <PanelLeftClose size={20} strokeWidth={1.5} class="shrink-0" />
        <span>Collapse</span>
      {/if}
    </button>
  </div>
</aside>