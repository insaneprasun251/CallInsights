<script lang="ts">
  import type { Snippet } from 'svelte';

  let { class: className = '', children }: { class?: string; children: Snippet } = $props();
</script>

<div class="rounded-lg border border-line bg-surface shadow-card {className}">
  {@render children()}
</div>