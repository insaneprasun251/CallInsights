import { HubConnectionBuilder } from '@microsoft/signalr';
import { getRecordings, uploadRecording, processRecording, deleteRecording } from '../services/recordingService';
import { ACCEPT_ATTRIBUTE, HUB_URL, RECORDINGS_CHANGED_EVENT } from '../utils/constants';
import { pickFiles } from '../utils/pickFiles';
import type { Recording } from '../types/recording';

class RecordingsStore {
  items = $state<Recording[]>([]);
  loading = $state(true);
  uploading = $state(false);
  busyFiles = $state<string[]>([]);
  error = $state('');
  query = $state('');
  selectedName = $state<string | null>(null);
  pendingDelete = $state<string | null>(null);
  deleting = $state(false);

  private failing = false;

  filtered = $derived.by(() => {
    const q = this.query.trim().toLowerCase();
    return q ? this.items.filter((r) => r.fileName.toLowerCase().includes(q)) : this.items;
  });

  selected = $derived(this.items.find((r) => r.fileName === this.selectedName) ?? null);

  async load() {
    try {
      this.items = await getRecordings();
      this.failing = false;
    } catch (e) {
      // report a lost connection once, not on every poll
      if (!this.failing) this.error = (e as Error).message;
      this.failing = true;
    } finally {
      this.loading = false;
    }
  }

  startLive(): () => void {
    this.load();

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();

    connection.on(RECORDINGS_CHANGED_EVENT, () => this.load());
    connection.onreconnected(() => this.load());

    let stopped = false;
    const connect = async () => {
      try {
        await connection.start();
        await this.load(); // catch anything that changed while we were not connected
      } catch {
        if (!stopped) setTimeout(connect, 5000);
      }
    };
    connect();

    return () => {
      stopped = true;
      connection.stop();
    };
  }

  async upload(files: File[]) {
    this.uploading = true;
    this.error = '';
    const problems: string[] = [];

    for (const file of files) {
      try {
        await uploadRecording(file);
      } catch (e) {
        problems.push(`${file.name}: ${(e as Error).message}`);
      }
    }

    if (problems.length) this.error = problems.join('  ·  ');
    this.uploading = false;
    await this.load();
  }

  async process(fileName: string) {
    this.error = '';
    this.busyFiles = [...this.busyFiles, fileName];
    try {
      await processRecording(fileName);
    } catch (e) {
      this.error = `${fileName}: ${(e as Error).message}`;
    } finally {
      this.busyFiles = this.busyFiles.filter((f) => f !== fileName);
      await this.load();
    }
  }

  chooseAndUpload() {
    pickFiles(ACCEPT_ATTRIBUTE, (files) => this.upload(files));
  }

  select(fileName: string | null) {
    this.selectedName = fileName;
  }

  requestDelete(fileName: string) {
    this.pendingDelete = fileName;
  }

  cancelDelete() {
    if (!this.deleting) this.pendingDelete = null;
  }

  async confirmDelete() {
    const name = this.pendingDelete;
    if (!name) return;

    this.deleting = true;
    this.error = '';
    try {
      await deleteRecording(name);
      if (this.selectedName === name) this.selectedName = null;
    } catch (e) {
      this.error = `${name}: ${(e as Error).message}`;
    } finally {
      this.deleting = false;
      this.pendingDelete = null;
      await this.load();
    }
  }

  clearError() {
    this.error = '';
  }
}

export const recordings = new RecordingsStore();