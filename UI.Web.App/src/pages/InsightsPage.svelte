<script lang="ts">
  import BrainCircuit from '@lucide/svelte/icons/brain-circuit';
  import FileAudio from '@lucide/svelte/icons/file-audio';
  import RefreshCw from '@lucide/svelte/icons/refresh-cw';

  import Alert from '../components/ui/Alert.svelte';
  import Button from '../components/ui/Button.svelte';
  import Card from '../components/ui/Card.svelte';
  import Chip from '../components/ui/Chip.svelte';
  import EmptyState from '../components/ui/EmptyState.svelte';

  import { recordings } from '../stores/recordings.svelte';
  import { getInsights } from '../services/insightsService';
  import { formatDate } from '../utils/format';

  import type { CallInsight } from '../types/insight';

  type JsonValue =
    | string
    | number
    | boolean
    | null
    | JsonValue[]
    | { [key: string]: JsonValue };

  let selectedCallId = $state<string | null>(null);
  let insights = $state<CallInsight[]>([]);
  let loadingInsights = $state(false);
  let error = $state('');

  const readyRecordings = $derived(
    recordings.items.filter(
      (recording) =>
        recording.status === 'Ready' && recording.callId !== null
    )
  );

  const selectedRecording = $derived(
    readyRecordings.find(
      (recording) => recording.callId === selectedCallId
    ) ?? null
  );

  async function viewInsights(callId: string) {
    selectedCallId = callId;
    loadingInsights = true;
    error = '';
    insights = [];

    try {
      const result = await getInsights(callId);

      // Ignore an older request if another recording was selected.
      if (selectedCallId === callId) {
        insights = result;
      }
    } catch (err) {
      if (selectedCallId === callId) {
        error =
          err instanceof Error
            ? err.message
            : 'Failed to load insights.';
      }
    } finally {
      if (selectedCallId === callId) {
        loadingInsights = false;
      }
    }
  }

  function parseResponse(responseText: string): JsonValue {
    try {
      return JSON.parse(responseText) as JsonValue;
    } catch {
      return responseText;
    }
  }

  function isObject(
    value: JsonValue
  ): value is { [key: string]: JsonValue } {
    return (
      value !== null &&
      typeof value === 'object' &&
      !Array.isArray(value)
    );
  }

  function objectEntries(
    value: { [key: string]: JsonValue }
  ): [string, JsonValue][] {
    return Object.entries(value) as [string, JsonValue][];
  }

  function formatLabel(key: string): string {
    return key
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/[_-]+/g, ' ')
      .replace(/\b\w/g, (character) => character.toUpperCase());
  }

  function formatValue(value: JsonValue): string {
    if (value === null) return '—';

    if (typeof value === 'string') {
      return value;
    }

    if (typeof value === 'object') {
      return JSON.stringify(value, null, 2) ?? '';
    }

    return String(value);
  }
</script>

<svelte:head>
  <title>Insights | CallInsights</title>
</svelte:head>

<div class="space-y-8">
  <header class="flex flex-wrap items-start justify-between gap-4">
    <div>
      <h1 class="text-2xl font-semibold tracking-tight">Insights</h1>
      <p class="mt-1 text-sm text-muted">
        Explore AI-generated insights from processed recordings.
      </p>
    </div>

    <Button
      variant="secondary"
      size="sm"
      disabled={recordings.loading}
      onclick={() => recordings.load()}
    >
      <RefreshCw size={15} strokeWidth={1.5} />
      Refresh recordings
    </Button>
  </header>

  {#if error}
    <Alert message={error} ondismiss={() => (error = '')} />
  {/if}

  <section>
    <div class="mb-3 flex items-baseline justify-between">
      <h2 class="text-sm font-semibold">Processed recordings</h2>

      {#if !recordings.loading}
        <span class="text-xs text-muted">
          {readyRecordings.length}
          {readyRecordings.length === 1 ? 'recording' : 'recordings'}
        </span>
      {/if}
    </div>

    <Card class="overflow-hidden">
      {#if recordings.loading}
        <div class="space-y-3 p-5" aria-busy="true">
          {#each [0, 1, 2] as item (item)}
            <div class="h-12 animate-pulse rounded-lg bg-canvas"></div>
          {/each}
        </div>
      {:else if readyRecordings.length === 0}
        <EmptyState
          icon={BrainCircuit}
          title="No processed recordings yet"
          description="Process a recording and wait until its status becomes Ready. Its saved insights will appear here."
        />
      {:else}
        <ul class="divide-y divide-line">
          {#each readyRecordings as recording (recording.callId)}
            <li class="flex flex-wrap items-center gap-3 px-4 py-4 sm:px-5">
              <div
                class="grid size-10 shrink-0 place-items-center rounded-lg bg-primary-tint text-primary-solid"
              >
                <FileAudio size={20} strokeWidth={1.5} />
              </div>

              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-medium text-ink">
                  {recording.fileName}
                </p>
                <p class="mt-1 text-xs text-muted">
                  Modified {formatDate(recording.modifiedUtc)}
                </p>
              </div>

              <Chip>Ready</Chip>

              {#if recording.callId}
                <Button
                  size="sm"
                  variant={selectedCallId === recording.callId
                    ? 'primary'
                    : 'secondary'}
                  disabled={loadingInsights}
                  onclick={() => viewInsights(recording.callId!)}
                >
                  {selectedCallId === recording.callId
                    ? 'Selected'
                    : 'View insights'}
                </Button>
              {/if}
            </li>
          {/each}
        </ul>
      {/if}
    </Card>
  </section>

  {#if selectedRecording}
    <section class="space-y-4">
      <header>
        <h2 class="text-lg font-semibold tracking-tight">
          Insights for {selectedRecording.fileName}
        </h2>
        <p class="mt-1 text-sm text-muted">
          Saved results from the language model.
        </p>
      </header>

      {#if loadingInsights}
        <Card class="p-5">
          <div class="space-y-3" aria-busy="true">
            <div class="h-4 w-48 animate-pulse rounded bg-canvas"></div>
            <div class="h-20 animate-pulse rounded-lg bg-canvas"></div>
            <div class="h-20 animate-pulse rounded-lg bg-canvas"></div>
          </div>
        </Card>
      {:else if insights.length === 0 && !error}
        <Card>
          <EmptyState
            icon={BrainCircuit}
            title="No saved insights found"
            description="This call has no saved prompt responses. Check that the insights step completed successfully."
          />
        </Card>
      {:else}
        <div class="space-y-4">
          {#each insights as insight (`${insight.templateKey}:${insight.templateVersion}`)}
            <Card class="space-y-4 p-5">
              <header class="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h3 class="text-sm font-semibold">
                    {formatLabel(insight.templateKey)}
                  </h3>
                  <p class="mt-1 text-xs text-muted">
                    Template version {insight.templateVersion}
                  </p>
                </div>

                <Chip>
                  {insight.responseModel ?? 'Unknown model'}
                </Chip>
              </header>

              {#if insight.responseText}
                {@const parsed = parseResponse(insight.responseText)}

                {#if isObject(parsed)}
                  <div class="grid gap-3 sm:grid-cols-2">
                    {#each objectEntries(parsed) as entry (entry[0])}
                      <div class="min-w-0 rounded-lg border border-line bg-surface p-4">
                        <h4 class="text-xs font-semibold text-muted">
                          {formatLabel(entry[0])}
                        </h4>
                        <div class="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-ink">
                          {formatValue(entry[1])}
                        </div>
                      </div>
                    {/each}
                  </div>
                {:else}
                  <pre class="overflow-x-auto whitespace-pre-wrap break-words rounded-lg bg-canvas p-4 text-sm leading-6">{formatValue(parsed)}</pre>
                {/if}
              {:else}
                <p class="text-sm text-muted">
                  No response has been saved for this prompt yet.
                </p>
              {/if}

              <footer class="border-t border-line pt-3 text-xs text-muted">
                Generated:
                {insight.respondedAtUtc
                  ? formatDate(insight.respondedAtUtc)
                  : 'Not generated yet'}
              </footer>
            </Card>
          {/each}
        </div>
      {/if}
    </section>
  {:else}
    <Card>
      <EmptyState
        icon={BrainCircuit}
        title="Select a recording"
        description="Choose View insights on a processed recording to see its saved AI analysis."
      />
    </Card>
  {/if}
</div>
