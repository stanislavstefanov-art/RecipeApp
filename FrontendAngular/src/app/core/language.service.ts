import { Injectable, effect, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { inject } from '@angular/core';

const STORAGE_KEY = 'lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  readonly current = signal<string>(localStorage.getItem(STORAGE_KEY) ?? 'bg');

  constructor() {
    this.translate.use(this.current());

    effect(() => {
      const lang = this.current();
      this.translate.use(lang);
      localStorage.setItem(STORAGE_KEY, lang);
    });
  }

  set(lang: string): void {
    this.current.set(lang);
  }
}
