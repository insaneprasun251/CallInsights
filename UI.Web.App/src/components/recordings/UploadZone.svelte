<script lang="ts">
  import Upload from '@lucide/svelte/icons/upload';
  import { ALLOWED_EXTENSIONS, ACCEPT_ATTRIBUTE } from '../../utils/constants';

  let { busy, onfiles }: { busy: boolean; onfiles: (files: File[]) => void } = $props();

  let dragging = $state(false);
  let input: HTMLInputElement;

  const formats = ALLOWED_EXTENSIONS.map((e) => e.slice(1).toUpperCase()).join(', ');

  function pick(list: FileList | null) {
    const files = list ? Array.from(list) : [];
    if (input) input.value = '';
    if (files.length) onfiles(files);
  }
</script>

<label
  class="flex cursor-pointer items-center gap-4 rounded-lg border border-dashed px-6 py-6 focus-within:ring-2 focus-within:ring-primary
    {dragging ? 'border-primary bg-primary-tint' : 'border-line bg-surface hover:border-muted'}
    {busy ? 'pointer-events-none opacity-60' : ''}"
  ondragover={(e) => {
    e.preventDefault();
    dragging = true;
  }}
  ondragleave={() => (dragging = false)}
  ondrop={(e) => {
    e.preventDefault();
    dragging = false;
    pick(e.dataTransfer?.files ?? null);
  }}
>
  <input
    bind:this={input}
    type="file"
    multiple
    accept={ACCEPT_ATTRIBUTE}
    class="sr-only"
    disabled={busy}
    onchange={() => pick(input.files)}
  />
  <span class="grid size-10 shrink-0 place-items-center rounded-full bg-primary-tint text-primary-solid">
    <Upload size={20} strokeWidth={1.5} />
  </span>
  <span>
    <span class="block text-sm font-medium">
      {busy ? 'Uploading…' : 'Drop recordings here or click to browse'}
    </span>
    <span class="block text-xs text-muted">{formats}</span>
  </span>
</label>