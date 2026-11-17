import type { Component } from 'svelte';
import AudioLines from '@lucide/svelte/icons/audio-lines';
import FileQuestion from '@lucide/svelte/icons/file-question';
import FileJson from '@lucide/svelte/icons/file-json';
import BrainCircuit from '@lucide/svelte/icons/brain-circuit';

import RecordingsPage from './pages/RecordingsPage.svelte';
import NotFoundPage from './pages/NotFoundPage.svelte';
import TranscriptionsPage from './pages/TranscriptionsPage.svelte';
import InsightsPage from './pages/InsightsPage.svelte';

import type { IconComponent } from './types/ui';

export interface AppRoute {
  path: string;
  title: string;
  icon: IconComponent;
  component: Component;
  nav: boolean; // show in the sidebar
}

// To add a page: create it in src/pages and add one line here.
export const routes: AppRoute[] = [
  { path: '/', title: 'Recordings', icon: AudioLines, component: RecordingsPage, nav: true },
  { path: '/transcriptions', title: 'Transcriptions', icon: FileJson, component: TranscriptionsPage, nav: true },
  { path: '/insights', title: 'Insights', icon: BrainCircuit, component: InsightsPage, nav: true }
];

const notFoundRoute: AppRoute = {
  path: '*',
  title: 'Not found',
  icon: FileQuestion,
  component: NotFoundPage,
  nav: false
};

export const resolveRoute = (path: string): AppRoute => routes.find((r) => r.path === path) ?? notFoundRoute;