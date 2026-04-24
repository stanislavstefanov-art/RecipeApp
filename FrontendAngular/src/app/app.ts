import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
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
