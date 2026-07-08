import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { FinanceService } from '../../../services/finance.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { SidebarComponent } from '../sidebar/sidebar';
import { FooterComponent } from '../../home/footer/footer';

@Component({
  selector: 'app-finance-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, SidebarComponent, FooterComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit {
  stats = signal<any>(null);
  recentPayments = signal<any[]>([]);
  loading = signal(true);
  errorMessage = signal('');

  constructor(private financeService: FinanceService, private router: Router) {}

  ngOnInit() {
    this.loadStats();
  }

  loadStats() {
    this.loading.set(true);
    this.financeService.getDashboardStats().subscribe({
      next: (statsRes: any) => {
        this.stats.set({
          totalTransactions: statsRes.totalTransactions,
          pendingApprovals: statsRes.pendingApprovals,
          totalRevenue: statsRes.totalRevenue,
          totalIntake: statsRes.totalIntake
        });
        
        this.financeService.getTransactions({ page: 1, size: 10 }).subscribe({
          next: (res: any) => {
            this.recentPayments.set(res.items || res);
            this.loading.set(false);
          },
          error: (err: any) => {
            this.handleError(err);
          }
        });
      },
      error: (err: any) => {
        this.handleError(err);
      }
    });
  }

  private handleError(err: any) {
    this.errorMessage.set('Failed to load dashboard data.');
    this.loading.set(false);
    const currentUrl = window.location.pathname + window.location.search;
    this.router.navigate(['/error'], { queryParams: { code: err.status || 500, returnUrl: currentUrl } });
  }
}
