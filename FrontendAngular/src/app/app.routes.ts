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
];
