import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-admin-password-reset',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './password-reset.html',
  styleUrl: './password-reset.css'
})
export class AdminPasswordResetComponent {
  public email = '';
  public otp = '';
  public newPassword = '';
  public confirmPassword = '';
  public showPassword = signal(false);

  public errorMessage = signal<string | null>(null);
  public successMessage = signal<string | null>(null);
  public isSubmitting = signal(false);

  // OTP state
  public isOtpSent = signal(false);
  public otpCountdown = signal(0);
  private timer: any;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  public togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  public sendOtp(): void {
    if (!this.email || !this.email.includes('@')) {
      this.errorMessage.set('Please enter a valid email to receive the OTP.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.authService.sendAdminOtp(this.email).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.isOtpSent.set(true);
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

    if (!this.email || !this.otp || !this.newPassword || !this.confirmPassword) {
      this.errorMessage.set('Please fill out all fields.');
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.errorMessage.set('Passwords do not match.');
      return;
    }

    this.isSubmitting.set(true);
    
    const payload = {
      email: this.email,
      otp: this.otp,
      newPassword: this.newPassword
    };

    this.authService.resetAdminPassword(payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Password reset successfully. Redirecting to login...');
        setTimeout(() => {
          this.router.navigate(['/admin/login']);
        }, 2000);
      },
      error: (err: any) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to reset password. Check OTP and try again.');
      }
    });
  }
}
