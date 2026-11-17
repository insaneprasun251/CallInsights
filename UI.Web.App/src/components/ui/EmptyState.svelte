<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { IconComponent } from '../../types/ui';

  let {
    icon: Icon,
    title,
    description,
    children
  }: { icon: IconComponent; title: string; description: string; children?: Snippet } = $props();
</script>

<div class="flex flex-col items-center px-6 py-16 text-center">
  <div class="mb-4 grid size-12 place-items-center rounded-full bg-primary-tint text-primary-solid">
    <Icon size={24} strokeWidth={1.5} />
  </div>
  <h3 class="text-sm font-semibold">{title}</h3>
  <p class="mt-1 max-w-sm text-sm text-muted">{description}</p>
  {#if children}
    <div class="mt-6">{@render children()}</div>
  {/if}
</div>