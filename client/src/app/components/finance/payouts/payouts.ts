import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FinanceService } from '../../../services/finance.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { SidebarComponent } from '../sidebar/sidebar';
import { FooterComponent } from '../../home/footer/footer';

@Component({
  selector: 'app-finance-payouts',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, SidebarComponent, FooterComponent],
  templateUrl: './payouts.html',
  styleUrl: './payouts.css'
})
export class PayoutsComponent implements OnInit {
  payouts = signal<any[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  errorMessage = signal('');

  Math = Math;

  filters = {
    status: 'All',
    sortBy: '',
    page: 1,
    size: 10
  };

  statusFilter = signal<string>('All');
  sortColumn = signal<string>('date');
  sortDirection = signal<'asc' | 'desc'>('desc');

  // Computed sorted payouts - sorting is handled server side for status/date, but we can leave this for small scale local sorting
  sortedPayouts = computed(() => {
    let data = [...this.payouts()];

    const col = this.sortColumn();
    const dir = this.sortDirection();
    
    if (col === 'amount') {
      return data.sort((a, b) => {
        return dir === 'asc'
          ? (a.amount || 0) - (b.amount || 0)
          : (b.amount || 0) - (a.amount || 0);
      });
    }
    return data;
  });

  constructor(private financeService: FinanceService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.statusFilter.set(params['status'] || 'All');
      this.filters.status = this.statusFilter();
      this.sortColumn.set(params['sortColumn'] || 'date');
      this.sortDirection.set((params['sortDirection'] as 'asc' | 'desc') || 'desc');
      this.filters.sortBy = `${this.sortColumn()}_${this.sortDirection()}`;
      this.filters.page = params['page'] ? +params['page'] : 1;
      
      this.loadPayouts();
    });
  }

  updateUrlParams() {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        status: this.statusFilter() !== 'All' ? this.statusFilter() : null,
        sortColumn: this.sortColumn() || null,
        sortDirection: this.sortDirection() || null,
        page: this.filters.page > 1 ? this.filters.page : null
      },
      queryParamsHandling: 'merge'
    });
  }

  loadPayouts() {
    this.loading.set(true);
    this.errorMessage.set('');
    this.financeService.getPayouts(this.filters).subscribe({
      next: (res: any) => {
        this.payouts.set(res.items || res);
        this.totalCount.set(res.totalCount || res.length || 0);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load payouts. Please try again.');
        this.loading.set(false);
      }
    });
  }

  setSort(column: string, direction: 'asc' | 'desc') {
    this.sortColumn.set(column);
    this.sortDirection.set(direction);
    this.filters.sortBy = `${column}_${direction}`;
    this.updateUrlParams();
  }

  onFilterChange() {
    this.filters.page = 1;
    this.updateUrlParams();
  }

  onStatusDropdownChange(event: Event) {
    const target = event.target as HTMLSelectElement;
    this.statusFilter.set(target.value);
    this.filters.status = target.value;
    this.onFilterChange();
  }

  changePage(delta: number) {
    this.filters.page += delta;
    this.updateUrlParams();
  }

  formatId(id: number): string {
    return String(id).slice(-5).padStart(5, '0');
  }

  getStatusClass(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'upcoming') return 'upcoming';
    if (s === 'completed') return 'completed';
    return '';
  }
}
