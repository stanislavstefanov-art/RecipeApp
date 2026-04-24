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
];
