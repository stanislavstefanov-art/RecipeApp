import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { RecipesClient } from '../../api/recipes.client';

@Component({
  selector: 'app-recipes-list',
  imports: [RouterLink],
  templateUrl: './recipes-list.html',
  styleUrl: './recipes-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesList {
  private readonly client = inject(RecipesClient);

  protected readonly recipes = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly isEmpty = computed(() => {
    const value = this.recipes.value();
    return value !== undefined && value.length === 0;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.recipes.error();
    if (!err) {
      return '';
    }
    return err instanceof Error ? err.message : String(err);
  });
}
