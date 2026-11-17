<script lang="ts">
  import { onMount } from 'svelte';
  import AppShell from './components/layout/AppShell.svelte';
  import { router } from './router/router.svelte';
  import { resolveRoute } from './routes';
  import { recordings } from './stores/recordings.svelte';

  const route = $derived(resolveRoute(router.path));
  const Page = $derived(route.component);

  $effect(() => {
    document.title = `${route.title} · Call Insights`;
  });

  onMount(() => recordings.startLive());
</script>

<AppShell>
  <Page />
</AppShell>