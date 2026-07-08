import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { FinanceService } from '../../../services/finance.service';

@Component({
  selector: 'app-finance-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginComponent {
  public loginForm: FormGroup;
  public otpForm: FormGroup;
  
  step = signal(1); // 1: Login, 2: OTP
  
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  constructor(private fb: FormBuilder, private financeService: FinanceService, private router: Router) {
    this.loginForm = this.fb.group({
      adminId: ['', Validators.required],
      password: ['', Validators.required]
    });
    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.pattern('^[0-9]{6}$')]]
    });
  }

  login() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      this.errorMessage.set('Finance ID and password are required.');
      this.successMessage.set('');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');
    const { adminId, password } = this.loginForm.value;
    this.financeService.financeLogin(adminId, password).subscribe({
      next: (res: any) => {
        if (res.otpRequired || res.OtpRequired) {
          this.step.set(2);
          this.loading.set(false);
          this.errorMessage.set('');
          this.successMessage.set(res.Message || res.message || 'OTP sent successfully.');
        } else if (res.token || res.Token) {
          const token = res.token || res.Token;
          localStorage.setItem('finance_token', token);
          this.loading.set(false);
          this.router.navigate(['/finance/dashboard']);
        }
      },
      error: (err: any) => {
        this.errorMessage.set(err.error?.Message || 'Invalid credentials.');
        this.loading.set(false);
      }
    });
  }

  verifyOtp() {
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      this.errorMessage.set('Valid 6-digit OTP is required.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');
    const otp = this.otpForm.value.otp;
    const adminId = this.loginForm.value.adminId;
    
    this.financeService.verifyFinanceLoginOtp(adminId, otp).subscribe({
      next: (res: any) => {
        const token = res.token || res.Token;
        if (token) {
          localStorage.setItem('finance_token', token);
        }
        this.loading.set(false);
        this.router.navigate(['/finance/dashboard']);
      },
      error: (err: any) => {
        this.errorMessage.set(err.error?.Message || 'Invalid OTP.');
        this.loading.set(false);
      }
    });
  }

  resendOtp() {
    this.errorMessage.set('');
    this.successMessage.set('');
    const adminId = this.loginForm.value.adminId;
    const password = this.loginForm.value.password;
    this.financeService.financeLogin(adminId, password).subscribe({
      next: (res: any) => {
        this.successMessage.set(res.Message || res.message || 'OTP resent successfully.');
      },
      error: (err: any) => {
        this.errorMessage.set(err.error?.Message || 'Failed to resend OTP.');
      }
    });
  }
}
