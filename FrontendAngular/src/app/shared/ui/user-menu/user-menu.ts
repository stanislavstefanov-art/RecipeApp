import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { session, logout } from '../../../core/auth.store';

@Component({
  selector: 'app-user-menu',
  imports: [TranslateModule],
  templateUrl: './user-menu.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserMenu {
  private readonly router = inject(Router);

  protected readonly session = session;
  protected readonly open = signal(false);

  protected toggleOpen(): void {
    this.open.update((v) => !v);
  }

  protected handleLogout(): void {
    logout();
    this.open.set(false);
    void this.router.navigate(['/login']);
  }
}
