import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';


// (Component metadata)
@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class RegisterComponent implements OnInit, OnDestroy {
  public name = '';
  public email = '';
  public mobileNumber = '';
  public password = '';
  public otp = '';
  public showPassword = signal(false);

  public togglePassword(): void {
    this.showPassword.update(s => !s);
  }
  
  // Consent policy checkbox
  public hasAcceptedPolicies = signal(false);
  public hasMarketingConsent = signal(false);

  // Consent modal state
  public showConsentModal = signal(false);
  public consentModalTitle = signal('');
  public consentModalContent = signal('');
  public isLoadingConsent = signal(false);

  public isOtpSent = signal(false);
  public isOtpVerified = signal(false);
  public isOtpInvalid = signal(false);
  public isVerifyingOtp = signal(false);
  public isSendingOtp = signal(false);
  public otpCountdown = signal(0);
  private countdownInterval: any = null;

  public errorMessage = signal<string | null>(null);
  public successMessage = signal<string | null>(null);
  public isSubmitting = signal(false);
  public returnUrl = '/';

  // Field-specific errors
  public nameError = signal<string | null>(null);
  public emailError = signal<string | null>(null);
  public otpError = signal<string | null>(null);
  public mobileError = signal<string | null>(null);
  public passwordError = signal<string | null>(null);
  public consentError = signal<string | null>(null);

  // Email Validation
  public isCheckingEmail = signal(false);
  public isEmailAvailable = signal<boolean | null>(null);
  private emailCheckSubject = new Subject<string>();

  // Active Terms ID fetched from backend
  public activeTermsId = signal<string>('10000');
  private subscriptions = new Subscription();

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    // Fetch active terms metadata to get the actual Terms_Id
    this.authService.getConsentDocument('General').subscribe({
      next: (doc) => {
        if (doc && doc.termsId) {
          this.activeTermsId.set(doc.termsId);
        }
      },
      error: (err) => {
        console.error('Failed to fetch active terms metadata on init', err);
      }
    });

    // Email check debounce
    this.subscriptions.add(
      this.emailCheckSubject.pipe(
        debounceTime(500),
        distinctUntilChanged()
      ).subscribe(email => {
        if (!email) {
          this.isEmailAvailable.set(null);
          this.emailError.set(null);
          this.isCheckingEmail.set(false);
          return;
        }

        this.isCheckingEmail.set(true);
        this.isEmailAvailable.set(null);
        this.emailError.set(null);

        this.authService.checkEmail(email).subscribe({
          next: (res) => {
            this.isCheckingEmail.set(false);
            if (res.exists) {
              this.isEmailAvailable.set(false);
              this.emailError.set('This email is already associated with an account.');
            } else {
              this.isEmailAvailable.set(true);
            }
          },
          error: () => {
            this.isCheckingEmail.set(false);
            this.emailError.set('Could not verify email availability.');
          }
        });
      })
    );
  }

  public onEmailChange(newEmail: string): void {
    this.email = newEmail;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!this.email || !emailRegex.test(this.email)) {
      this.emailError.set('Please enter a valid email address.');
      this.isEmailAvailable.set(null);
    } else {
      this.emailError.set(null);
      this.emailCheckSubject.next(newEmail);
    }
  }

  public validateName(): void {
    if (!this.name || !this.name.trim()) {
      this.nameError.set('Full name is required.');
    } else {
      this.nameError.set(null);
    }
  }

  public validateMobile(): void {
    if (this.mobileNumber) {
      const mobileRegex = /^\d{10}$/;
      if (!mobileRegex.test(this.mobileNumber)) {
        this.mobileError.set('Please enter a valid 10-digit mobile number.');
      } else {
        this.mobileError.set(null);
      }
    } else {
      this.mobileError.set('Mobile number is required.');
    }
  }

  public validatePassword(): void {
    if (!this.password || this.password.length < 6) {
      this.passwordError.set('Password must be at least 6 characters.');
    } else {
      this.passwordError.set(null);
    }
  }

  ngOnDestroy(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
  }

  private clearErrors(): void {
    this.nameError.set(null);
    this.emailError.set(null);
    this.otpError.set(null);
    this.mobileError.set(null);
    this.passwordError.set(null);
    this.consentError.set(null);
    this.errorMessage.set(null);
  }

  public sendOtp(): void {
    this.clearErrors();
    if (!this.email) {
      this.emailError.set('Please enter your email to receive an OTP.');
      return;
    }

    if (this.isEmailAvailable() === false) {
      this.emailError.set('Cannot register with this email address.');
      return;
    }

    this.isSendingOtp.set(true);

    this.authService.sendOtp(this.email, 'registration').subscribe({
      next: () => {
        this.isSendingOtp.set(false);
        this.isOtpSent.set(true);
        this.startOtpCountdown(30);
      },
      error: (err) => {
        this.isSendingOtp.set(false);
        this.emailError.set(err.error?.message || 'Failed to send OTP. Please check your email.');
      }
    });
  }

  private startOtpCountdown(seconds: number): void {
    this.otpCountdown.set(seconds);
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
    this.countdownInterval = setInterval(() => {
      const current = this.otpCountdown();
      if (current <= 1) {
        this.otpCountdown.set(0);
        clearInterval(this.countdownInterval);
      } else {
        this.otpCountdown.set(current - 1);
      }
    }, 1000);
  }

  public onOtpChange(val: string): void {
    this.isOtpVerified.set(false);
    this.isOtpInvalid.set(false);
    this.otpError.set(null);

    if (val && val.length === 6) {
      this.isVerifyingOtp.set(true);
      this.authService.verifyOtp(this.email, val, 'registration').subscribe({
        next: (res) => {
          this.isVerifyingOtp.set(false);
          this.isOtpVerified.set(true);
          this.isOtpInvalid.set(false);
        },
        error: (err) => {
          this.isVerifyingOtp.set(false);
          this.isOtpVerified.set(false);
          this.isOtpInvalid.set(true);
          this.otpError.set(err.error?.message || 'Invalid OTP code.');
        }
      });
    }
  }

  private parseMarkdown(content: string): string {
    if (!content) return '';
    
    // Split into lines to filter out policy metadata
    let lines = content.split('\n');
    lines = lines.filter(line => {
      const trimmed = line.trim().toLowerCase();
      return !trimmed.startsWith('**version:**') &&
             !trimmed.startsWith('**policy id:**') &&
             !trimmed.startsWith('version:') &&
             !trimmed.startsWith('policy id:');
    });
    
    let parsedText = lines.join('\n');
    
    // Parse Headers
    parsedText = parsedText
      .replace(/^# (.*?)$/gm, '<h1>$1</h1>')
      .replace(/^## (.*?)$/gm, '<h2>$1</h2>')
      .replace(/^### (.*?)$/gm, '<h3>$1</h3>')
      .replace(/^#### (.*?)$/gm, '<h4>$1</h4>');
      
    // Parse Horizontal Rules (---)
    parsedText = parsedText.replace(/^---$/gm, '<hr class="markdown-hr"/>');
    
    // Parse Bold text (**text**)
    parsedText = parsedText.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    
    // Parse Bullet Lists (* item)
    parsedText = parsedText.replace(/^\* (.*?)$/gm, '<li>$1</li>');
    
    // Convert double line breaks into paragraphs
    parsedText = parsedText.split('\n\n').map(p => {
      p = p.trim();
      if (!p) return '';
      if (p.startsWith('<h') || p.startsWith('<hr') || p.startsWith('<li')) {
        return p;
      }
      return `<p>${p}</p>`;
    }).join('\n');
    
    // Convert remaining single line breaks to <br/>
    parsedText = parsedText.replace(/\n/g, '<br/>');
    
    return parsedText;
  }

  /**
   * Fetch and display consent details in a popup modal
   */
  public openConsent(type: string, event: Event): void {
    event.preventDefault();
    this.isLoadingConsent.set(true);
    this.showConsentModal.set(true);

    // Map the client-side type string to the backend policy type: General
    const backendType = (type === 'terms' || type === 'data_consent') ? 'General' : type;

    this.authService.getConsentDocument(backendType).subscribe({
      next: (doc) => {
        // doc.filePath is e.g. "/assets/policies/G10001.md"
        const fileUrl = doc.filePath.startsWith('http') ? doc.filePath : `${environment.serverUrl}${doc.filePath}`;
        this.http.get(fileUrl, { responseType: 'text' }).subscribe({
          next: (content) => {
            this.consentModalTitle.set(type === 'terms' ? 'Terms and Conditions' : 'Data Storage & Privacy Consent');
            const formatted = this.parseMarkdown(content);
            this.consentModalContent.set(formatted);
            this.isLoadingConsent.set(false);
          },
          error: (err) => {
            console.error('Failed to fetch policy file content', err);
            this.consentModalTitle.set('Error');
            this.consentModalContent.set('<p>Unable to load the consent document contents.</p>');
            this.isLoadingConsent.set(false);
          }
        });
      },
      error: (err) => {
        console.error('Failed to fetch consent policy metadata', err);
        this.consentModalTitle.set('Error');
        this.consentModalContent.set('<p>Unable to retrieve the requested document metadata from the server.</p>');
        this.isLoadingConsent.set(false);
      }
    });
  }

  public closeConsentModal(): void {
    this.showConsentModal.set(false);
    this.consentModalTitle.set('');
    this.consentModalContent.set('');
  }

  public onSubmit(event: Event): void {
    event.preventDefault();
    this.clearErrors();

    let hasError = false;

    // Validate Full Name
    if (!this.name || !this.name.trim()) {
      this.nameError.set('Full name is required.');
      hasError = true;
    }

    // Validate email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!this.email || !emailRegex.test(this.email)) {
      this.emailError.set('Please enter a valid email address.');
      hasError = true;
    }

    // Validate OTP status
    if (!this.isOtpSent()) {
      this.otpError.set('Please request and verify an OTP first.');
      hasError = true;
    } else if (!this.isOtpVerified()) {
      this.otpError.set('Please verify your OTP code before submitting.');
      hasError = true;
    }

    // Validate mobile number
    const mobileRegex = /^\d{10}$/;
    if (!this.mobileNumber || !mobileRegex.test(this.mobileNumber)) {
      this.mobileError.set('Please enter a valid 10-digit mobile number.');
      hasError = true;
    }

    // Validate password
    if (!this.password || this.password.length < 6) {
      this.passwordError.set('Password must be at least 6 characters.');
      hasError = true;
    }

    // Validate consent checkbox
    if (!this.hasAcceptedPolicies()) {
      this.consentError.set('You must accept the Terms & Conditions and Data Consent Policy.');
      hasError = true;
    }

    if (hasError) {
      return;
    }

    this.isSubmitting.set(true);

    const payload = {
      name: this.name,
      email: this.email,
      mobileNumber: this.mobileNumber,
      password: this.password,
      consentedTermsId: this.activeTermsId(), // Use the dynamically retrieved active terms ID
      hasMarketingConsent: this.hasMarketingConsent(),
      otp: this.otp
    };

    this.authService.register(payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        if (typeof window !== 'undefined') {
          localStorage.setItem('justRegistered', 'true');
        }
        this.router.navigateByUrl(this.returnUrl);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = err.error?.message || 'Registration failed. Please check details or OTP.';
        
        // Distribute error to corresponding fields if matching keywords, otherwise set general error
        const msgLower = msg.toLowerCase();
        if (msgLower.includes('email') || msgLower.includes('registered')) {
          this.emailError.set(msg);
        } else if (msgLower.includes('otp') || msgLower.includes('verification')) {
          this.otpError.set(msg);
        } else if (msgLower.includes('mobile') || msgLower.includes('number')) {
          this.mobileError.set(msg);
        } else if (msgLower.includes('password')) {
          this.passwordError.set(msg);
        } else if (msgLower.includes('terms') || msgLower.includes('consent')) {
          this.consentError.set(msg);
        } else if (msgLower.includes('name')) {
          this.nameError.set(msg);
        } else {
          this.errorMessage.set(msg);
        }
      }
    });
  }
}
