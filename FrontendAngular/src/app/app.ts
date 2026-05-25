import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { Toast } from './shared/ui/toast';
import { UserMenu } from './shared/ui/user-menu/user-menu';
import { ThemeService } from './core/theme.service';
import { LanguageService } from './core/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Toast, TranslateModule, UserMenu],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly isNavOpen = signal(false);

  constructor() {
    inject(ThemeService);
    inject(LanguageService);
  }

  protected toggleNav(): void {
    this.isNavOpen.update((v) => !v);
  }

  protected closeNav(): void {
    this.isNavOpen.set(false);
  }
}
