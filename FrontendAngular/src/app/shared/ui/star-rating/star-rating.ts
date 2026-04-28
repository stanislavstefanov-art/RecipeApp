import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-star-rating',
  templateUrl: './star-rating.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StarRatingComponent {
  readonly value = input<number | null>(null);
  readonly readonly = input<boolean>(false);
  readonly size = input<'sm' | 'md'>('md');
  readonly rated = output<number>();

  protected readonly stars = [1, 2, 3, 4, 5] as const;

  protected isFilled(n: number): boolean {
    const v = this.value();
    return v != null && n <= Math.round(v);
  }

  protected onSelect(n: number): void {
    if (!this.readonly()) {
      this.rated.emit(n);
    }
  }
}
