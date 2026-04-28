import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { isAuthenticated } from './auth.store';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  return isAuthenticated() ? true : router.parseUrl('/login');
};
