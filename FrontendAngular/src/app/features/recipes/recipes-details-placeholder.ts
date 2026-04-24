import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-recipes-details-placeholder',
  imports: [RouterLink],
  template: `
    <section class="mx-auto max-w-xl p-6">
      <a routerLink="/recipes" class="text-sm text-gray-500">&larr; Back to recipes</a>
      <h1 class="mt-4 text-2xl font-semibold">Recipe {{ id() }}</h1>
      <p class="mt-2 text-sm text-gray-500">Details view arrives in slice 3.</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesDetailsPlaceholder {
  readonly id = input.required<string>();
}
