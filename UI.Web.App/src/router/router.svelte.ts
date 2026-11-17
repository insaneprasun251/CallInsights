function currentPath(): string {
  return window.location.hash.replace(/^#/, '') || '/';
}

class Router {
  path = $state(currentPath());

  constructor() {
    window.addEventListener('hashchange', () => (this.path = currentPath()));
  }

  navigate(to: string) {
    window.location.hash = to;
  }
}

export const router = new Router();