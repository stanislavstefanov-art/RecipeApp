import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { RecipesClient } from '../../api/recipes.client';
import { getErrorMessage } from '../../shared/get-error-message';
import { StarRatingComponent } from '../../shared/ui/star-rating/star-rating';

@Component({
  selector: 'app-recipes-list',
  imports: [RouterLink, TranslateModule, StarRatingComponent],
  templateUrl: './recipes-list.html',
  styleUrl: './recipes-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesList {
  private readonly client = inject(RecipesClient);
  private readonly translate = inject(TranslateService);

  protected readonly recipes = rxResource({
    stream: () => this.client.list(),
  });

  protected readonly isEmpty = computed(() => {
    const value = this.recipes.value();
    return value !== undefined && value.length === 0;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.recipes.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });
}
