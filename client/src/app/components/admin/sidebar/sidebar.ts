import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class AdminSidebarComponent {
  public isCollapsed = typeof localStorage !== 'undefined' && localStorage.getItem('adminSidebarCollapsed') === 'true';

  public toggleSidebar() {
    this.isCollapsed = !this.isCollapsed;
    localStorage.setItem('adminSidebarCollapsed', String(this.isCollapsed));
  }
}
