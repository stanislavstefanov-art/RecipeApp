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
];
