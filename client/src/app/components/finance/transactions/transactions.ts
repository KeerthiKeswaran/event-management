import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink,  ActivatedRoute, Router } from '@angular/router';
import { FinanceService } from '../../../services/finance.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { SidebarComponent } from '../sidebar/sidebar';
import { FooterComponent } from '../../home/footer/footer';

@Component({
  selector: 'app-finance-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, SidebarComponent, FooterComponent],
  templateUrl: './transactions.html',
  styleUrl: '../dashboard/dashboard.css' // Reuse dashboard CSS
})
export class TransactionsComponent implements OnInit {
  transactions = signal<any[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  errorMessage = signal('');

  Math = Math;

  filters = {
    transactionType: '',
    status: '',
    startDate: '',
    endDate: '',
    sortBy: '',
    page: 1,
    size: 10
  };

  sortColumn = 'date';
  sortDirection: 'asc' | 'desc' = 'desc';

  constructor(private financeService: FinanceService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.filters.transactionType = params['transactionType'] || '';
      this.filters.status = params['status'] || '';
      this.filters.startDate = params['startDate'] || '';
      this.filters.endDate = params['endDate'] || '';
      this.sortColumn = params['sortColumn'] || 'date';
      this.sortDirection = (params['sortDirection'] as 'asc' | 'desc') || 'desc';
      this.filters.sortBy = `${this.sortColumn}_${this.sortDirection}`;
      this.filters.page = params['page'] ? +params['page'] : 1;
      
      this.loadTransactions();
    });
  }

  updateUrlParams() {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        transactionType: this.filters.transactionType || null,
        status: this.filters.status || null,
        startDate: this.filters.startDate || null,
        endDate: this.filters.endDate || null,
        sortColumn: this.sortColumn || null,
        sortDirection: this.sortDirection || null,
        page: this.filters.page > 1 ? this.filters.page : null
      },
      queryParamsHandling: 'merge'
    });
  }

  loadTransactions() {
    this.loading.set(true);
    this.financeService.getTransactions(this.filters).subscribe({
      next: (res: any) => {
        this.transactions.set(res.items || res);
        this.totalCount.set(res.totalCount || res.length || 0);
        this.loading.set(false);
      },
      error: (err: any) => {
        this.errorMessage.set('Failed to load transactions.');
        this.loading.set(false);
      }
    });
  }

  onFilterChange() {
    this.filters.page = 1;
    this.updateUrlParams();
  }

  clearFilters() {
    this.filters = {
      transactionType: '',
      status: '',
      startDate: '',
      endDate: '',
      sortBy: 'date_desc',
      page: 1,
      size: 10
    };
    this.sortColumn = 'date';
    this.sortDirection = 'desc';
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  toggleSort(column: string) {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.filters.sortBy = `${this.sortColumn}_${this.sortDirection}`;
    this.onFilterChange();
  }

  changePage(delta: number) {
    this.filters.page += delta;
    this.updateUrlParams();
  }
}
