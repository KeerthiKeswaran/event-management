import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-password-reset',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './password-reset.html',
  styleUrl: './password-reset.css'
})
export class PasswordResetComponent {
  public resetForm: FormGroup;
  public showPassword = signal(false);

  public errorMessage = signal<string | null>(null);
  public successMessage = signal<string | null>(null);
  public isSubmitting = signal(false);

  // OTP state
  public isOtpSent = signal(false);
  public otpCountdown = signal(0);
  private timer: any;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.resetForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      otp: [''], // Added later based on state
      newPassword: [''],
      confirmPassword: ['']
    }, { validators: this.passwordMatchValidator });
  }

  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const p1 = control.get('newPassword')?.value;
    const p2 = control.get('confirmPassword')?.value;
    if (p1 && p2 && p1 !== p2) {
      control.get('confirmPassword')?.setErrors({ mismatch: true });
      return { mismatch: true };
    }
    return null;
  }

  public togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  public sendOtp(): void {
    const emailCtrl = this.resetForm.get('email');
    if (!emailCtrl || emailCtrl.invalid) {
      emailCtrl?.markAsTouched();
      this.errorMessage.set('Please enter a valid email to receive the OTP.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const email = emailCtrl.value;

    this.authService.sendOtp(email, 'password-reset').subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.isOtpSent.set(true);
        
        // Add required validators to other fields now that OTP is sent
        this.resetForm.get('otp')?.setValidators([Validators.required, Validators.pattern('^[0-9]{6}$')]);
        this.resetForm.get('newPassword')?.setValidators([Validators.required, Validators.minLength(8), Validators.pattern('^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{8,}$')]);
        this.resetForm.get('confirmPassword')?.setValidators([Validators.required]);
        this.resetForm.updateValueAndValidity();

        this.successMessage.set('OTP has been sent to your email.');
        this.startOtpCountdown();
      },
      error: (err: any) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to send OTP. Please try again.');
      }
    });
  }

  private startOtpCountdown(): void {
    this.otpCountdown.set(60);
    clearInterval(this.timer);
    this.timer = setInterval(() => {
      const current = this.otpCountdown();
      if (current > 0) {
        this.otpCountdown.set(current - 1);
      } else {
        clearInterval(this.timer);
      }
    }, 1000);
  }

  public onSubmit(event: Event): void {
    event.preventDefault();
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      this.errorMessage.set('Please fill out all fields correctly.');
      return;
    }

    this.isSubmitting.set(true);
    
    const { email, otp, newPassword } = this.resetForm.value;

    // First verify OTP, then reset password
    this.authService.verifyOtp(email, otp, 'password-reset').subscribe({
      next: () => {
        const payload = {
          email: email,
          newPassword: newPassword
        };
        this.authService.resetUserPassword(payload).subscribe({
          next: () => {
            this.isSubmitting.set(false);
            this.successMessage.set('Password reset successfully. Redirecting to login...');
            setTimeout(() => {
              this.router.navigate(['/login']);
            }, 2000);
          },
          error: (err: any) => {
            this.isSubmitting.set(false);
            this.errorMessage.set(err.error?.message || 'Failed to reset password. Please try again.');
          }
        });
      },
      error: (err: any) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message || 'Invalid OTP. Please check and try again.');
      }
    });
  }
}
