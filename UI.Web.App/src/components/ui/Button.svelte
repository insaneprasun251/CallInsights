<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { HTMLButtonAttributes } from 'svelte/elements';

  interface Props extends HTMLButtonAttributes {
    variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
    size?: 'sm' | 'md';
    children: Snippet;
  }

  let { variant = 'primary', size = 'md', class: className = '', children, ...rest }: Props = $props();

  const variants = {
    primary: 'bg-primary-solid text-white hover:bg-primary-solid-hover',
    secondary: 'border border-line bg-surface text-ink hover:bg-canvas',
    ghost: 'text-muted hover:bg-canvas hover:text-ink',
    danger: 'bg-high-text text-white hover:brightness-90'
  };
  const sizes = { sm: 'h-8 px-3 text-xs', md: 'h-10 px-4 text-sm' };
</script>

<button
  type="button"
  class="inline-flex shrink-0 items-center justify-center gap-2 rounded-lg font-medium disabled:cursor-not-allowed disabled:opacity-50 {variants[variant]} {sizes[size]} {className}"
  {...rest}
>
  {@render children()}
</button>