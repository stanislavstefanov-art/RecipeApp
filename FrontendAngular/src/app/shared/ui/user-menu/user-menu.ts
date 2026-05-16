import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { session, logout } from '../../../core/auth.store';
import { UserClient } from '../../../api/user.client';

@Component({
  selector: 'app-user-menu',
  imports: [TranslateModule],
  templateUrl: './user-menu.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserMenu {
  private readonly router = inject(Router);
  private readonly userClient = inject(UserClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);

  protected readonly session = session;
  protected readonly open = signal(false);
  protected readonly isClearing = signal(false);

  protected toggleOpen(): void {
    this.open.update((v) => !v);
  }

  protected handleLogout(): void {
    logout();
    this.open.set(false);
    void this.router.navigate(['/login']);
  }

  protected handleClearAllData(): void {
    const confirmed = window.confirm(this.translate.instant('common.confirmClearAllData'));
    if (!confirmed) return;

    this.isClearing.set(true);
    this.userClient
      .clearAllData()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isClearing.set(false);
          this.open.set(false);
          void this.router.navigate(['/recipes']);
        },
        error: () => {
          this.isClearing.set(false);
        },
      });
  }
}
