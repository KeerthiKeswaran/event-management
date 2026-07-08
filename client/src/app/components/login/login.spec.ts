import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginComponent } from './login';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { By } from '@angular/platform-browser';
import { ReactiveFormsModule } from '@angular/forms';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent, ReactiveFormsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParams: { returnUrl: '/dashboard' } } }
        },
        {
          provide: AuthService,
          useValue: { login: vi.fn() }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create the login component', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with an invalid form', () => {
    expect(component.loginForm.valid).toBeFalsy();
    expect(component.loginForm.get('email')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
  });

  it('should validate email field as required and email format', () => {
    const emailControl = component.loginForm.get('email');
    
    // Check required error
    emailControl?.setValue('');
    expect(emailControl?.hasError('required')).toBeTruthy();

    // Check invalid email format
    emailControl?.setValue('invalidemail');
    expect(emailControl?.hasError('email')).toBeTruthy();

    // Check valid email
    emailControl?.setValue('test@example.com');
    expect(emailControl?.valid).toBeTruthy();
  });

  it('should show error messages when submit is clicked on empty form', () => {
    const formElement = fixture.debugElement.query(By.css('form'));
    formElement.triggerEventHandler('ngSubmit', { preventDefault: vi.fn() });
    fixture.detectChanges();

    // Form should remain invalid
    expect(component.loginForm.invalid).toBeTruthy();
    
    // Check that error spans are rendered after touched
    const errorSpans = fixture.nativeElement.querySelectorAll('.error-text span');
    expect(errorSpans.length).toBeGreaterThan(0);
    expect(authService.login).not.toHaveBeenCalled();
  });

  it('should call authService.login and navigate on successful login', () => {
    const loginSpy = vi.spyOn(authService, 'login').mockReturnValue(of({ token: 'abc' }));
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');

    component.loginForm.setValue({
      email: 'user@example.com',
      password: 'password123'
    });

    const formElement = fixture.debugElement.query(By.css('form'));
    formElement.triggerEventHandler('ngSubmit', { preventDefault: vi.fn() });

    expect(loginSpy).toHaveBeenCalledWith('user@example.com', 'password123');
    expect(component.isSubmitting()).toBeFalsy();
    expect(navigateSpy).toHaveBeenCalledWith('/dashboard');
  });

  it('should display error message on login failure', () => {
    const errorResponse = { error: { message: 'Invalid credentials' } };
    const loginSpy = vi.spyOn(authService, 'login').mockReturnValue(throwError(() => errorResponse));

    component.loginForm.setValue({
      email: 'user@example.com',
      password: 'wrongpassword'
    });

    const formElement = fixture.debugElement.query(By.css('form'));
    formElement.triggerEventHandler('ngSubmit', { preventDefault: vi.fn() });
    fixture.detectChanges();

    expect(loginSpy).toHaveBeenCalledWith('user@example.com', 'wrongpassword');
    expect(component.errorMessage()).toBe('Invalid credentials');
    
    const alertDiv = fixture.debugElement.query(By.css('.alert-error'));
    expect(alertDiv).toBeTruthy();
    expect(alertDiv.nativeElement.textContent).toContain('Invalid credentials');
    expect(component.isSubmitting()).toBeFalsy();
  });

  it('should toggle password visibility when button is clicked', () => {
    expect(component.showPassword()).toBeFalsy();
    
    const toggleButton = fixture.debugElement.query(By.css('.password-toggle'));
    toggleButton.triggerEventHandler('click', null);
    
    expect(component.showPassword()).toBeTruthy();
    
    // Input type should now be text
    fixture.detectChanges();
    const passwordInput = fixture.debugElement.query(By.css('#password'));
    expect(passwordInput.nativeElement.type).toBe('text');
  });
});
