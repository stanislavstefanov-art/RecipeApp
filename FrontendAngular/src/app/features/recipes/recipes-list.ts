import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { RecipesClient } from '../../api/recipes.client';
import { getErrorMessage } from '../../shared/get-error-message';
import { StarRatingComponent } from '../../shared/ui/star-rating/star-rating';

@Component({
  selector: 'app-recipes-list',
  imports: [RouterLink, TranslateModule, StarRatingComponent, FormsModule],
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

  protected readonly filterType = signal<number | null>(null);
  protected readonly filterSource = signal<'all' | 'manual' | 'imported'>('all');
  protected readonly filterOrigin = signal<'all' | 'home' | 'borrowed' | 'bought'>('all');
  protected readonly filterMinStars = signal<number | null>(null);
  protected readonly filterIngredient = signal('');

  protected readonly allIngredients = computed(() => {
    const list = this.recipes.value() ?? [];
    const set = new Set<string>();
    for (const r of list) {
      for (const name of r.ingredientNames) {
        set.add(name.toLowerCase());
      }
    }
    return [...set].sort();
  });

  protected readonly ingredientSuggestions = computed(() => {
    const q = this.filterIngredient().trim().toLowerCase();
    if (q.length < 2) return [];
    return this.allIngredients().filter((n) => n.includes(q)).slice(0, 8);
  });

  protected readonly filteredRecipes = computed(() => {
    const list = this.recipes.value() ?? [];
    const type = this.filterType();
    const source = this.filterSource();
    const origin = this.filterOrigin();
    const minStars = this.filterMinStars();
    const ingredient = this.filterIngredient().trim().toLowerCase();

    return list.filter((r) => {
      if (type !== null && r.recipeType !== type) return false;
      if (source === 'manual' && r.isImported) return false;
      if (source === 'imported' && !r.isImported) return false;
      if (origin === 'home' && r.origin !== 1) return false;
      if (origin === 'borrowed' && r.origin !== 2) return false;
      if (origin === 'bought' && r.origin !== 3) return false;
      if (minStars !== null) {
        if (r.averageStars === null || r.averageStars < minStars) return false;
      }
      if (ingredient.length > 0) {
        const match = r.ingredientNames.some((n) => n.toLowerCase().includes(ingredient));
        if (!match) return false;
      }
      return true;
    });
  });

  protected readonly hasActiveFilter = computed(
    () =>
      this.filterType() !== null ||
      this.filterSource() !== 'all' ||
      this.filterOrigin() !== 'all' ||
      this.filterMinStars() !== null ||
      this.filterIngredient().trim().length > 0,
  );

  protected readonly isEmpty = computed(() => {
    const value = this.recipes.value();
    return value !== undefined && value.length === 0;
  });

  protected readonly noResults = computed(
    () => !this.isEmpty() && this.filteredRecipes().length === 0 && this.recipes.value() !== undefined,
  );

  protected readonly errorMessage = computed(() => {
    const err = this.recipes.error();
    return err ? getErrorMessage(err, this.translate) : '';
  });

  protected setIngredient(value: string): void {
    this.filterIngredient.set(value);
  }

  protected clearFilters(): void {
    this.filterType.set(null);
    this.filterSource.set('all');
    this.filterOrigin.set('all');
    this.filterMinStars.set(null);
    this.filterIngredient.set('');
  }
}
