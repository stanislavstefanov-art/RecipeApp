import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { RecipesClient } from '../../api/recipes.client';
import { UpdateRecipeNameForm } from './update-recipe-name-form';

@Component({
  selector: 'app-recipes-details',
  imports: [RouterLink, UpdateRecipeNameForm],
  templateUrl: './recipes-details.html',
  styleUrl: './recipes-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipesDetails {
  private readonly client = inject(RecipesClient);

  readonly id = input.required<string>();

  protected readonly recipe = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.client.get(params),
  });

  protected onNameSaved(): void {
    this.recipe.reload();
  }

  protected readonly isNotFound = computed(() => {
    const err = this.recipe.error();
    return err instanceof HttpErrorResponse && err.status === 404;
  });

  protected readonly errorMessage = computed(() => {
    const err = this.recipe.error();
    if (!err || this.isNotFound()) {
      return '';
    }
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as { title?: string; detail?: string } | null;
      return problem?.detail ?? problem?.title ?? err.message;
    }
    return err instanceof Error ? err.message : String(err);
  });

  protected readonly hasIngredients = computed(() => {
    const value = this.recipe.value();
    return value !== undefined && value.ingredients.length > 0;
  });

  protected readonly hasSteps = computed(() => {
    const value = this.recipe.value();
    return value !== undefined && value.steps.length > 0;
  });
}
