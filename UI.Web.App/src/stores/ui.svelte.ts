class UiState {
  sidebarCollapsed = $state(false);
  paletteOpen = $state(false);

  toggleSidebar() {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }
}

export const ui = new UiState();