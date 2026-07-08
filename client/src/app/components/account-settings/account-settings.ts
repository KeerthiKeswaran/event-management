import { Component, OnInit, OnDestroy, signal, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subscription, of, Subject } from 'rxjs';
import { delay, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { AppStoreService } from '../../store/app-store.service';
import { ActionTypes } from '../../store/actions/app.actions';
import { UserModel } from '../../models/user.model';
import { NavbarComponent } from '../home/navbar/navbar';
import { FooterComponent } from '../home/footer/footer';
import { AuthService } from '../../services/auth.service';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent],
  templateUrl: './account-settings.html',
  styleUrl: './account-settings.css'
})
export class AccountSettingsComponent implements OnInit, OnDestroy {
  private store = inject(AppStoreService);
  private router = inject(Router);
  private http = inject(HttpClient);
  private adminService = inject(AdminService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private subscriptions = new Subscription();

  public isAdmin = signal(false);
  public isFinance = signal(false);

  // Active Tab
  public activeTab = signal<'profile' | 'password' | 'close'>('profile');

  // User Profile State
  public currentUser: UserModel | null = null;
  public profileName = '';
  public profileEmail = '';
  public profilePhone = '';

  // Profile Edit states
  public isEditingProfile = signal(false);
  public isSavingProfile = signal(false);
  public profileSuccessMessage = signal<string | null>(null);
  public profileErrorMessage = signal<string | null>(null);

  // Email Validation
  public emailCheckError = signal<string | null>(null);

  public showProfileOtpModal = signal(false);
  public profileOtpCode = '';
  public isVerifyingProfileOtp = signal(false);
  public profileOtpError = signal<string | null>(null);

  public profileOtpCountdown = signal(0);
  private profileCountdownInterval: any = null;


  // Password reset flow states
  public showPasswordOtpModal = signal(false);
  public showNewPasswordModal = signal(false);
  public passwordOtpCode = '';
  
  public newPassword = '';
  public confirmPassword = '';
  
  public isRequestingPasswordOtp = signal(false);
  public isVerifyingPasswordOtp = signal(false);
  public isSubmittingNewPassword = signal(false);
  
  public passwordSuccessMessage = signal<string | null>(null);
  public passwordErrorMessage = signal<string | null>(null);
  public passwordOtpError = signal<string | null>(null);
  public passwordResetError = signal<string | null>(null);

  public passwordOtpCountdown = signal(0);
  private passwordCountdownInterval: any = null;

  // Close Account States
  public selectedReason = '';
  public otherReasonExplanation = '';
  public typedConfirmName = '';
  public otpCode = '';
  
  public isOtpSent = signal(false);
  public isSubmittingClosure = signal(false);
  public closureErrorMessage = signal<string | null>(null);
  
  // Final Thanking screen state
  public isAccountClosed = signal(false);

  // Available closure reasons
  public closureReasons = [
    { value: 'no_longer_using', label: 'I am no longer using this platform' },
    { value: 'privacy_concerns', label: 'I have privacy or data storage concerns' },
    { value: 'too_many_emails', label: 'I receive too many promotional emails' },
    { value: 'better_alternative', label: 'I found a better alternative event platform' },
    { value: 'other', label: 'Other (Please specify below)' }
  ];

  ngOnInit(): void {
    this.isAdmin.set(this.router.url.includes('/admin'));
    this.isFinance.set(this.router.url.includes('/finance'));

    // Redirect if not logged in
    const tokenKey = this.isAdmin() ? 'admin_token' : this.isFinance() ? 'finance_token' : 'user_token';
    const token = typeof window !== 'undefined' ? localStorage.getItem(tokenKey) : null;
    
    if (!token && !this.isAccountClosed()) {
      if (this.isAdmin()) {
        this.router.navigate(['/admin/login']);
      } else if (this.isFinance()) {
        this.router.navigate(['/finance/login']);
      } else {
        this.router.navigate(['/login']);
      }
      return;
    }

    // Watch user profile
    this.subscriptions.add(
      this.store.select(state => state.auth.user).subscribe(user => {
        if (user) {
          this.currentUser = user;
          this.profileName = user.name || (user as any).Name || '';
          this.profileEmail = user.email || (user as any).Email || '';
          this.profilePhone = user.mobile_Number || (user as any).Mobile_Number || '';
        } else if (this.isAdmin() || this.isFinance()) {
          if (this.isAdmin()) {
            this.adminService.getAdminProfile().subscribe({
              next: (profile) => {
                this.profileName = profile.name || profile.Name || '';
                this.profileEmail = profile.email || profile.Email || '';
                this.profilePhone = profile.mobile_Number || profile.Mobile_Number || '';
                this.currentUser = { id: 0, name: this.profileName, email: this.profileEmail, mobile_Number: this.profilePhone };
                this.cdr.detectChanges();
              },
              error: () => {}
            });
          } else {
            let tokenKey = 'user_token';
            if (typeof window !== 'undefined') {
              if (window.location.pathname.startsWith('/admin')) tokenKey = 'admin_token';
              else if (window.location.pathname.startsWith('/finance')) tokenKey = 'finance_token';
            }
            const token = typeof window !== 'undefined' ? localStorage.getItem(tokenKey) : null;
            if (token) {
              try {
                const payload = JSON.parse(atob(token.split('.')[1]));
                const name = payload.name || payload.unique_name || payload.nameid || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || '';
                const email = payload.email || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '';
                this.profileName = name;
                this.profileEmail = email;
                this.currentUser = { id: 0, name: name, email: email };
              } catch(e) {}
            }
          }
        }
        this.cdr.detectChanges();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.passwordCountdownInterval) {
      clearInterval(this.passwordCountdownInterval);
    }
    if (this.profileCountdownInterval) {
      clearInterval(this.profileCountdownInterval);
    }
  }

  // 1. Update Profile - Enable/Disable Editing Mode
  public setTab(tab: 'profile' | 'password' | 'close'): void {
    if (this.activeTab() === 'profile' && tab !== 'profile') {
      this.cancelEditProfile();
    }
    this.activeTab.set(tab);
  }

  public enableEditProfile(): void {
    this.isEditingProfile.set(true);
    this.profileSuccessMessage.set(null);
    this.profileErrorMessage.set(null);
  }

  public cancelEditProfile(): void {
    this.isEditingProfile.set(false);
    if (this.currentUser) {
      this.profileName = this.currentUser.name;
      this.profileEmail = this.currentUser.email;
      this.profilePhone = this.currentUser.mobile_Number || '';
    }
    this.profileErrorMessage.set(null);
    this.emailCheckError.set(null);
  }

  public onEmailChange(newEmail: string): void {
    this.profileEmail = newEmail;
    this.emailCheckError.set(null);
  }

  public updateProfile(): void {
    if (!this.profileName.trim() || !this.profileEmail.trim()) {
      this.profileErrorMessage.set('Name and Email are required.');
      return;
    }

    if (this.profileEmail !== this.currentUser?.email) {
      // First check if new email is available
      this.isSavingProfile.set(true);
      this.profileSuccessMessage.set(null);
      this.profileErrorMessage.set(null);
      this.emailCheckError.set(null);

      this.authService.checkEmail(this.profileEmail).subscribe({
        next: (res) => {
          if (res.exists) {
            this.isSavingProfile.set(false);
            this.emailCheckError.set('This email is already associated with another account.');
          } else {
            // Email is available, proceed to OTP
            this.authService.sendOtp(this.profileEmail, 'email-change').subscribe({
              next: () => {
                this.isSavingProfile.set(false);
                this.showProfileOtpModal.set(true);
                this.profileOtpCode = '';
                this.startProfileOtpCountdown(30);
              },
              error: (err) => {
                this.isSavingProfile.set(false);
                this.profileErrorMessage.set(err.error?.message || 'Failed to send verification code. Please try again.');
              }
            });
          }
        },
        error: () => {
          this.isSavingProfile.set(false);
          this.emailCheckError.set('Could not verify email availability. Please try again.');
        }
      });
    } else {
      // Just name/phone change, no OTP needed
      this.submitProfileUpdate();
    }
  }

  public resendProfileOtp(): void {
    this.profileOtpError.set(null);
    this.authService.sendOtp(this.profileEmail, 'email-change').subscribe({
      next: () => {
        this.startProfileOtpCountdown(30);
      },
      error: (err) => {
        this.profileOtpError.set(err.error?.message || 'Failed to resend verification code.');
      }
    });
  }

  private startProfileOtpCountdown(seconds: number): void {
    this.profileOtpCountdown.set(seconds);
    if (this.profileCountdownInterval) {
      clearInterval(this.profileCountdownInterval);
    }
    this.profileCountdownInterval = setInterval(() => {
      const current = this.profileOtpCountdown();
      if (current <= 1) {
        this.profileOtpCountdown.set(0);
        clearInterval(this.profileCountdownInterval);
      } else {
        this.profileOtpCountdown.set(current - 1);
      }
    }, 1000);
  }

  public verifyProfileOtp(): void {
    if (!this.profileOtpCode || this.profileOtpCode.length !== 6) {
      this.profileOtpError.set('Please enter a valid 6-digit verification code.');
      return;
    }
    
    this.isVerifyingProfileOtp.set(true);
    this.profileOtpError.set(null);
    this.submitProfileUpdate(this.profileOtpCode);
  }

  public closeProfileOtpModal(): void {
    this.showProfileOtpModal.set(false);
    this.profileOtpCode = '';
    this.profileOtpError.set(null);
  }

  private submitProfileUpdate(otp?: string): void {
    this.isSavingProfile.set(true);
    this.profileSuccessMessage.set(null);
    this.profileErrorMessage.set(null);

    const payload = {
      name: this.profileName,
      mobileNumber: this.profilePhone,
      email: this.profileEmail,
      otp: otp
    };

    this.authService.updateProfile(payload).subscribe({
      next: () => {
        this.isSavingProfile.set(false);
        this.isEditingProfile.set(false);
        if (otp) {
          this.closeProfileOtpModal();
          this.isVerifyingProfileOtp.set(false);
        }
        this.profileSuccessMessage.set('Profile details updated successfully.');
        
        // Dispatch to update global state
        const updatedUser = {
          ...this.currentUser,
          name: this.profileName,
          email: this.profileEmail,
          mobile_Number: this.profilePhone
        } as UserModel;

        this.store.dispatch({
          type: ActionTypes.LOAD_USER_PROFILE_SUCCESS,
          payload: updatedUser
        });

        setTimeout(() => this.profileSuccessMessage.set(null), 4000);
      },
      error: (err) => {
        this.isSavingProfile.set(false);
        if (otp) {
          this.isVerifyingProfileOtp.set(false);
          this.profileOtpError.set(err.error?.message || 'Invalid or expired OTP.');
        } else {
          this.profileErrorMessage.set(err.error?.message || 'Failed to save profile. Please try again.');
        }
      }
    });
  }

  // 2. Password Reset Flow:
  // Step A: Trigger Password Change and send OTP
  public requestPasswordChange(): void {
    this.isRequestingPasswordOtp.set(true);
    this.passwordErrorMessage.set(null);
    this.passwordOtpError.set(null);
    
    const email = this.currentUser?.email;
    if (!email) return;

    const request$ = this.isAdmin() 
      ? this.authService.sendAdminOtp(email) 
      : this.authService.sendOtp(email, 'password-reset');

    request$.subscribe({
      next: () => {
        this.isRequestingPasswordOtp.set(false);
        this.showPasswordOtpModal.set(true);
        this.startPasswordOtpCountdown(30);
      },
      error: (err) => {
        this.isRequestingPasswordOtp.set(false);
        this.passwordErrorMessage.set(err.error?.message || 'Failed to request verification code. Please try again.');
      }
    });
  }

  public resendPasswordOtp(): void {
    this.passwordOtpError.set(null);
    const email = this.currentUser?.email;
    if (!email) return;

    this.isRequestingPasswordOtp.set(true);
    const request$ = this.isAdmin() 
      ? this.authService.sendAdminOtp(email) 
      : this.authService.sendOtp(email, 'password-reset');

    request$.subscribe({
      next: () => {
        this.isRequestingPasswordOtp.set(false);
        this.startPasswordOtpCountdown(30);
      },
      error: (err) => {
        this.isRequestingPasswordOtp.set(false);
        this.passwordOtpError.set(err.error?.message || 'Failed to resend verification code.');
      }
    });
  }

  private startPasswordOtpCountdown(seconds: number): void {
    this.passwordOtpCountdown.set(seconds);
    if (this.passwordCountdownInterval) {
      clearInterval(this.passwordCountdownInterval);
    }
    this.passwordCountdownInterval = setInterval(() => {
      const current = this.passwordOtpCountdown();
      if (current <= 1) {
        this.passwordOtpCountdown.set(0);
        clearInterval(this.passwordCountdownInterval);
      } else {
        this.passwordOtpCountdown.set(current - 1);
      }
    }, 1000);
  }

  // Step B: Verify the verification OTP
  public verifyPasswordOtp(): void {
    if (!this.passwordOtpCode || this.passwordOtpCode.length !== 6) {
      this.passwordOtpError.set('Please enter a valid 6-digit verification code.');
      return;
    }

    this.isVerifyingPasswordOtp.set(true);
    this.passwordOtpError.set(null);

    const email = this.currentUser?.email;
    if (!email) return;

    const purpose = this.isAdmin() ? 'admin-password-reset' : 'password-reset';
    
    this.authService.verifyOtp(email, this.passwordOtpCode, purpose).subscribe({
      next: () => {
        this.isVerifyingPasswordOtp.set(false);
        this.showPasswordOtpModal.set(false);
        this.showNewPasswordModal.set(true);
        
        this.newPassword = '';
        this.confirmPassword = '';
        this.passwordResetError.set(null);
      },
      error: (err) => {
        this.isVerifyingPasswordOtp.set(false);
        this.passwordOtpError.set(err.error?.message || 'Invalid OTP code. Verification failed.');
      }
    });
  }

  // Step C: Submit New Password & Reset Password
  public submitNewPassword(): void {
    if (!this.newPassword || !this.confirmPassword) {
      this.passwordResetError.set('Please fill in all password fields.');
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.passwordResetError.set('Passwords do not match.');
      return;
    }

    if (this.newPassword.length < 6) {
      this.passwordResetError.set('Password must be at least 6 characters.');
      return;
    }

    this.isSubmittingNewPassword.set(true);
    this.passwordResetError.set(null);

    const email = this.currentUser?.email;
    if (!email) return;

    const payload = {
      email,
      otp: this.passwordOtpCode,
      newPassword: this.newPassword
    };

    const request$ = this.isAdmin()
      ? this.authService.resetAdminPassword(payload)
      : this.authService.resetPassword(payload);

    request$.subscribe({
      next: () => {
        this.isSubmittingNewPassword.set(false);
        this.showNewPasswordModal.set(false);
        this.passwordSuccessMessage.set('Password successfully updated.');
        
        setTimeout(() => this.passwordSuccessMessage.set(null), 5000);
      },
      error: (err) => {
        this.isSubmittingNewPassword.set(false);
        this.passwordResetError.set(err.error?.message || 'Failed to update password. Please try again.');
      }
    });
  }

  public closePasswordOtpModal(): void {
    this.showPasswordOtpModal.set(false);
    this.passwordOtpCode = '';
    this.passwordOtpError.set(null);
  }

  public closeNewPasswordModal(): void {
    this.showNewPasswordModal.set(false);
    this.newPassword = '';
    this.confirmPassword = '';
    this.passwordResetError.set(null);
  }

  // 3. Close Account - Step 1: Send OTP
  public requestAccountClosure(): void {
    if (!this.selectedReason) {
      this.closureErrorMessage.set('Please select a reason for closing your account.');
      return;
    }

    if (this.selectedReason === 'other' && !this.otherReasonExplanation.trim()) {
      this.closureErrorMessage.set('Please explain your reason in the text box.');
      return;
    }

    if (this.typedConfirmName.trim() !== this.currentUser?.name) {
      this.closureErrorMessage.set('Verification Name does not match your profile name.');
      return;
    }

    this.isSubmittingClosure.set(true);
    this.closureErrorMessage.set(null);

    // MOCK OTP send logic (matching register flow behaviour)
    of({ success: true }).pipe(delay(1000)).subscribe({
      next: () => {
        this.isSubmittingClosure.set(false);
        this.isOtpSent.set(true);
      },
      error: () => {
        this.isSubmittingClosure.set(false);
        this.closureErrorMessage.set('An error occurred. Please try again.');
      }
    });
  }

  // 3. Close Account - Step 2: Confirm OTP & Close
  public confirmAccountClosure(): void {
    if (!this.otpCode || this.otpCode.length !== 6) {
      this.closureErrorMessage.set('Please enter a valid 6-digit OTP code.');
      return;
    }

    this.isSubmittingClosure.set(true);
    this.closureErrorMessage.set(null);

    // COMMENTED OUT: Actual API Call to backend for closure with OTP confirmation:
    /*
    const closurePayload = {
      reason: this.selectedReason,
      explanation: this.selectedReason === 'other' ? this.otherReasonExplanation : '',
      confirmName: this.typedConfirmName,
      otp: this.otpCode
    };
    this.http.post('/api/user/close-account', closurePayload).subscribe({
      next: () => {
        // success handler
      },
      error: (err) => {
        // error handler
      }
    });
    */

    // Simulate OTP Verification Success
    of({ success: true }).pipe(delay(1200)).subscribe({
      next: () => {
        this.isSubmittingClosure.set(false);
        this.isAccountClosed.set(true);

        // Clear local storage completely
        if (typeof window !== 'undefined') {
          localStorage.clear();
        }

        // Dispatch logout action to clear global store state
        this.store.dispatch({ type: ActionTypes.LOGOUT });
      },
      error: () => {
        this.isSubmittingClosure.set(false);
        this.closureErrorMessage.set('Invalid OTP. Please try again.');
      }
    });
  }

  // Cancel OTP verification and go back to reason select
  public cancelClosureOtp(): void {
    this.isOtpSent.set(false);
    this.otpCode = '';
    this.closureErrorMessage.set(null);
  }

  public navigateHome(): void {
    this.router.navigate(['/']);
  }
}
