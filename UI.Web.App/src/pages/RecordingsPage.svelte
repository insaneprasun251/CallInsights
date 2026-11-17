<script lang="ts">
  import AudioLines from '@lucide/svelte/icons/audio-lines';
  import SearchX from '@lucide/svelte/icons/search-x';
  import Upload from '@lucide/svelte/icons/upload';
  import Alert from '../components/ui/Alert.svelte';
  import Button from '../components/ui/Button.svelte';
  import Card from '../components/ui/Card.svelte';
  import EmptyState from '../components/ui/EmptyState.svelte';
  import SidePanel from '../components/ui/SidePanel.svelte';
  import UploadZone from '../components/recordings/UploadZone.svelte';
  import RecordingRow from '../components/recordings/RecordingRow.svelte';
  import RecordingDetails from '../components/recordings/RecordingDetails.svelte';
  import ConfirmDialog from '../components/ui/ConfirmDialog.svelte';
  import { recordings } from '../stores/recordings.svelte';
</script>

<div class="space-y-8">
  <header>
    <h1 class="text-2xl font-semibold tracking-tight">Recordings</h1>
    <p class="mt-1 text-sm text-muted">
      Recordings from your call storage. Process one to start the analysis.
    </p>
  </header>

  <UploadZone busy={recordings.uploading} onfiles={(files) => recordings.upload(files)} />

  {#if recordings.error}
    <Alert message={recordings.error} ondismiss={() => recordings.clearError()} />
  {/if}

  <section>
    <div class="mb-3 flex items-baseline justify-between">
      <h2 class="text-sm font-semibold">All recordings</h2>
      {#if !recordings.loading}
        <span class="text-xs text-muted">{recordings.filtered.length} of {recordings.items.length}</span>
      {/if}
    </div>

    <Card class="overflow-hidden">
      {#if recordings.loading}
        <div class="divide-y divide-line" aria-busy="true">
          {#each [0, 1, 2] as i (i)}
            <div class="flex h-11 items-center gap-4 px-4">
              <div class="size-5 animate-pulse rounded bg-canvas"></div>
              <div class="h-3 w-48 animate-pulse rounded bg-canvas"></div>
            </div>
          {/each}
        </div>
      {:else if recordings.items.length === 0}
        <EmptyState
          icon={AudioLines}
          title="No recordings yet"
          description="Upload a call recording to see it here. You can process it once it appears."
        >
          <Button onclick={() => recordings.chooseAndUpload()}>
            <Upload size={16} strokeWidth={1.5} />
            Upload recording
          </Button>
        </EmptyState>
      {:else if recordings.filtered.length === 0}
        <EmptyState
          icon={SearchX}
          title="No matches"
          description={`Nothing matches "${recordings.query.trim()}". Try a different name.`}
        />
      {:else}
        <ul class="divide-y divide-line">
          {#each recordings.filtered as r (r.fileName)}
            <RecordingRow
              recording={r}
              selected={recordings.selectedName === r.fileName}
              busy={recordings.busyFiles.includes(r.fileName)}
              onopen={() => recordings.select(r.fileName)}
              onprocess={() => recordings.process(r.fileName)}
              ondelete={() => recordings.requestDelete(r.fileName)}
            />
          {/each}
        </ul>
      {/if}
    </Card>
  </section>
</div>

<SidePanel
  open={recordings.selected !== null}
  title="Recording details"
  onclose={() => recordings.select(null)}
>
  {#if recordings.selected}
    <RecordingDetails recording={recordings.selected} />
  {/if}
</SidePanel>

<ConfirmDialog
  open={recordings.pendingDelete !== null}
  title="Delete recording?"
  message={`"${recordings.pendingDelete ?? ''}" will be permanently removed, along with its transcript and analysis if it was processed.`}
  busy={recordings.deleting}
  onconfirm={() => recordings.confirmDelete()}
  oncancel={() => recordings.cancelDelete()}
/>