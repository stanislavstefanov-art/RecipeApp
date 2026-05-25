import { Injectable, effect, signal } from '@angular/core';

export type Theme = 'warm' | 'fresh' | 'indigo';

const STORAGE_KEY = 'app.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly current = signal<Theme>(
    (localStorage.getItem(STORAGE_KEY) as Theme | null) ?? 'warm',
  );

  constructor() {
    effect(() => {
      const theme = this.current();
      if (theme === 'warm') {
        document.documentElement.removeAttribute('data-theme');
      } else {
        document.documentElement.setAttribute('data-theme', theme);
      }
      localStorage.setItem(STORAGE_KEY, theme);
    });
  }

  set(theme: Theme): void {
    this.current.set(theme);
  }
}
