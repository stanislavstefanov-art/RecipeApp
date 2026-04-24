import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'recipes',
  },
  {
    path: 'recipes',
    loadComponent: () => import('./features/recipes/recipes-list').then((m) => m.RecipesList),
  },
  {
    path: 'recipes/new',
    loadComponent: () =>
      import('./features/recipes/recipes-create').then((m) => m.RecipesCreate),
  },
  {
    path: 'recipes/import',
    loadComponent: () =>
      import('./features/recipes/recipes-import').then((m) => m.RecipesImport),
  },
  {
    path: 'recipes/:id',
    loadComponent: () =>
      import('./features/recipes/recipes-details').then((m) => m.RecipesDetails),
  },
  {
    path: 'persons',
    loadComponent: () =>
      import('./features/persons/persons-list').then((m) => m.PersonsList),
  },
  {
    path: 'persons/new',
    loadComponent: () =>
      import('./features/persons/persons-create').then((m) => m.PersonsCreate),
  },
  {
    path: 'persons/:id',
    loadComponent: () =>
      import('./features/persons/persons-details').then((m) => m.PersonsDetails),
  },
  {
    path: 'households',
    loadComponent: () =>
      import('./features/households/households-list').then((m) => m.HouseholdsList),
  },
  {
    path: 'households/new',
    loadComponent: () =>
      import('./features/households/households-create').then((m) => m.HouseholdsCreate),
  },
  {
    path: 'households/:id',
    loadComponent: () =>
      import('./features/households/households-details').then((m) => m.HouseholdsDetails),
  },
  {
    path: 'meal-plans',
    loadComponent: () =>
      import('./features/meal-plans/meal-plans-list').then((m) => m.MealPlansList),
  },
  {
    path: 'meal-plans/new',
    loadComponent: () =>
      import('./features/meal-plans/meal-plans-create').then((m) => m.MealPlansCreate),
  },
  {
    path: 'meal-plans/suggest',
    loadComponent: () =>
      import('./features/meal-plans/meal-plans-suggest').then((m) => m.MealPlansSuggest),
  },
  {
    path: 'meal-plans/:id',
    loadComponent: () =>
      import('./features/meal-plans/meal-plans-details').then((m) => m.MealPlansDetails),
  },
];
