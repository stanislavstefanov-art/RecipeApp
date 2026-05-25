import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { Toast } from './shared/ui/toast';
import { LanguageSwitcher } from './shared/ui/language-switcher/language-switcher';
import { UserMenu } from './shared/ui/user-menu/user-menu';
import { ThemeSwitcher } from './shared/ui/theme-switcher/theme-switcher';
import { ThemeService } from './core/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Toast, TranslateModule, LanguageSwitcher, UserMenu, ThemeSwitcher],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly isNavOpen = signal(false);

  constructor() {
    inject(ThemeService);
  }

  protected toggleNav(): void {
    this.isNavOpen.update((v) => !v);
  }

  protected closeNav(): void {
    this.isNavOpen.set(false);
  }
}
