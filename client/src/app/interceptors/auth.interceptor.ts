import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  let tokenKey = 'user_token';
  if (typeof window !== 'undefined') {
    const path = window.location.pathname;
    if (path.startsWith('/admin')) {
      tokenKey = 'admin_token';
    } else if (path.startsWith('/finance')) {
      tokenKey = 'finance_token';
    }
  }
  const token = typeof window !== 'undefined' ? localStorage.getItem(tokenKey) : null;
  
  // Only intercept local API requests to avoid leaking headers to external geocoding/image APIs
  if (req.url.includes('/api/') && !req.url.includes('wikipedia.org')) {
    let headers: any = {
      'ngrok-skip-browser-warning': 'true'
    };
    
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const cloned = req.clone({
      setHeaders: headers
    });
    return next(cloned);
  }
  
  return next(req);
};
