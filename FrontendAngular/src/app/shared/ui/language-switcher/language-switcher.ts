import { ChangeDetectionStrategy, Component, inject, signal, effect } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  templateUrl: './language-switcher.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSwitcher {
  private readonly translate = inject(TranslateService);

  protected readonly activeLang = signal<string>(
    localStorage.getItem('lang') ?? 'bg'
  );

  constructor() {
    const saved = localStorage.getItem('lang') ?? 'bg';
    this.translate.use(saved);
    this.activeLang.set(saved);

    effect(() => {
      const lang = this.activeLang();
      this.translate.use(lang);
      localStorage.setItem('lang', lang);
    });
  }

  protected setLang(lang: string): void {
    this.activeLang.set(lang);
  }
}
