<script lang="ts">
  import type { Snippet } from 'svelte';

  let { class: className = '', children }: { class?: string; children: Snippet } = $props();
</script>

<span class="inline-flex h-6 items-center rounded-md bg-canvas px-2 text-xs text-muted {className}">
  {@render children()}
</span>