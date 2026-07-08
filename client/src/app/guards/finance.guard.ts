import { inject } from '@angular/core';
import { Router } from '@angular/router';

export const financeGuard = () => {
  const router = inject(Router);
  const token = typeof window !== 'undefined' ? localStorage.getItem('finance_token') : null;

  if (token) {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role;
      if (role === 'finance') {
        return true;
      }
    } catch {
      return router.parseUrl('/finance/login');
    }
  }

  return router.parseUrl('/finance/login');
};
