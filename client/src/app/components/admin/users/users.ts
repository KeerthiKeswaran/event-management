import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { AdminSidebarComponent } from '../sidebar/sidebar';
import { catchError, finalize, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { of, Subject } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, AdminSidebarComponent],
  templateUrl: './users.html',
  styleUrl: './users.css'
})
export class AdminUsersComponent implements OnInit {
  public users = signal<any[]>([]);
  public totalUsers = signal<number>(0);
  public loading = signal<boolean>(true);
  public error = signal<string | null>(null);

  // Filters and Pagination
  public keyword = signal<string>('');
  private searchSubject = new Subject<string>();

  public filterStatus = signal<string>('All Status');
  public filterStartDate = signal<string>('');
  public filterEndDate = signal<string>('');
  public currentPage = signal<number>(1);
  public pageSize = signal<number>(10);
  
  // Sort State: newest, oldest, name_asc, name_desc
  public sortOption = signal<string>('newest');

  public totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.totalUsers() / this.pageSize()));
  });

  // Modal state
  public showModal = signal<boolean>(false);
  public selectedUser = signal<any | null>(null);
  public newStatus = signal<string>('');
  public updatingStatus = signal<boolean>(false);
  public updateError = signal<string | null>(null);
  public showSuccessAnimation = signal<boolean>(false);

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.keyword.set(searchTerm);
      this.applyFilters();
    });

    this.loadUsers();
  }

  public onSearchInput(value: string) {
    this.keyword.set(value);
    this.searchSubject.next(value);
  }

  public loadUsers() {
    this.loading.set(true);
    this.error.set(null);

    const start = this.filterStartDate() ? new Date(this.filterStartDate()).toISOString() : undefined;
    const end = this.filterEndDate() ? new Date(this.filterEndDate()).toISOString() : undefined;

    this.adminService.getUsers(
      this.keyword() || undefined,
      this.filterStatus() !== 'All Status' ? this.filterStatus() : undefined,
      start,
      end,
      this.sortOption(),
      this.currentPage(),
      this.pageSize()
    ).pipe(
      catchError((err: HttpErrorResponse) => {
        this.error.set(err.error?.Message || 'Failed to load users');
        return of(null);
      }),
      finalize(() => this.loading.set(false))
    ).subscribe((res: any) => {
      if (res) {
        this.users.set(res.items || []);
        this.totalUsers.set(res.totalCount || 0);
      }
    });
  }

  public applyFilters() {
    this.currentPage.set(1);
    this.loadUsers();
  }

  public clearFilters() {
    this.keyword.set('');
    this.filterStatus.set('All Status');
    this.filterStartDate.set('');
    this.filterEndDate.set('');
    this.sortOption.set('newest');
    this.currentPage.set(1);
    this.loadUsers();
  }

  public toggleSortName() {
    if (this.sortOption() === 'name_asc') {
      this.sortOption.set('name_desc');
    } else {
      this.sortOption.set('name_asc');
    }
    this.currentPage.set(1);
    this.loadUsers();
  }

  public toggleSortDate() {
    if (this.sortOption() === 'newest') {
      this.sortOption.set('oldest');
    } else {
      this.sortOption.set('newest');
    }
    this.currentPage.set(1);
    this.loadUsers();
  }

  public getSortIcon(column: 'name' | 'date'): string {
    const current = this.sortOption();
    if (column === 'name') {
      if (current === 'name_asc') return '▲';
      if (current === 'name_desc') return '▼';
    } else {
      if (current === 'newest') return '▼';
      if (current === 'oldest') return '▲';
    }
    return '';
  }

  public nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadUsers();
    }
  }

  public prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadUsers();
    }
  }

  public openUserModal(user: any) {
    this.selectedUser.set(user);
    this.newStatus.set(user.status || user.Status);
    this.updateError.set(null);
    this.showModal.set(true);
  }

  public closeUserModal() {
    this.showModal.set(false);
    this.showSuccessAnimation.set(false);
    this.selectedUser.set(null);
  }

  public get isStatusChanged(): boolean {
    const user = this.selectedUser();
    if (!user) return false;
    return this.newStatus() !== (user.status || user.Status);
  }

  public updateStatus() {
    const user = this.selectedUser();
    if (!user || !this.isStatusChanged) return;

    this.updatingStatus.set(true);
    this.updateError.set(null);

    const targetUserId = user.user_Id || user.User_Id;
    this.adminService.updateUserStatus(targetUserId, this.newStatus()).pipe(
      catchError((err: HttpErrorResponse) => {
        this.updateError.set(err.error?.Message || 'Failed to update user status');
        return of(null);
      }),
      finalize(() => this.updatingStatus.set(false))
    ).subscribe((res: any) => {
      if (res) {
        // Success
        this.showSuccessAnimation.set(true);
        setTimeout(() => {
          this.loadUsers();
          this.closeUserModal();
        }, 1500);
      }
    });
  }

  public getStatusBadgeClass(status: string): string {
    if (!status) return 'badge-inactive';
    const s = status.toLowerCase();
    if (s === 'active') return 'badge-active';
    if (s === 'restricted') return 'badge-warning';
    return 'badge-inactive';
  }
}
