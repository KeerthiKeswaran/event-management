import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-finance-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class SidebarComponent {
  isCollapsed = typeof localStorage !== 'undefined' && localStorage.getItem('financeSidebarCollapsed') === 'true';

  toggleSidebar() {
    this.isCollapsed = !this.isCollapsed;
    localStorage.setItem('financeSidebarCollapsed', String(this.isCollapsed));
  }
}
