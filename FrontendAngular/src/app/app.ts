import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { Toast } from './shared/ui/toast';
import { LanguageSwitcher } from './shared/ui/language-switcher/language-switcher';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Toast, TranslateModule, LanguageSwitcher],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly isNavOpen = signal(false);

  protected toggleNav(): void {
    this.isNavOpen.update((v) => !v);
  }

  protected closeNav(): void {
    this.isNavOpen.set(false);
  }
}
