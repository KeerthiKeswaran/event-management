import { inject } from '@angular/core';
import { Router } from '@angular/router';

export const adminGuard = () => {
  const router = inject(Router);
  const token = typeof window !== 'undefined' ? localStorage.getItem('admin_token') : null;

  if (token) {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role;
      if (role === 'admin') {
        return true;
      }
    } catch {
      return router.parseUrl('/admin/login');
    }
  }

  return router.parseUrl('/admin/login');
};
