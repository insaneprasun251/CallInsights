<script lang="ts">
  import FileQuestion from '@lucide/svelte/icons/file-question';
  import Card from '../components/ui/Card.svelte';
  import EmptyState from '../components/ui/EmptyState.svelte';
</script>

<Card>
  <EmptyState
    icon={FileQuestion}
    title="Page not found"
    description="This page doesn't exist. Use the sidebar to get back on track."
  />
</Card>