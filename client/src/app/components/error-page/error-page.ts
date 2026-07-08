import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';


@Component({
  selector: 'app-error-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './error-page.html',
  styleUrl: './error-page.css'
})
export class ErrorPageComponent implements OnInit, OnDestroy {
  public errorCode = signal<number>(500);
  private pollingInterval: any;
  private returnUrl: string = '/';

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['code']) {
        this.errorCode.set(Number(params['code']));
      }
      if (params['returnUrl']) {
        this.returnUrl = params['returnUrl'];
      }
    });

    this.checkServer();
    // Poll the server every 5 seconds
    this.pollingInterval = setInterval(() => {
      this.checkServer();
    }, 2000);
  }

  private checkServer(): void {
    this.http.get(`${environment.apiUrl}/Health`).subscribe({
      next: () => {
        // Server is back online! Auto-redirect to home.
        this.router.navigateByUrl(this.returnUrl);
      },
      error: () => {
        // Still down. Remain on the error page.
      }
    });
  }

  ngOnDestroy(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }
}
