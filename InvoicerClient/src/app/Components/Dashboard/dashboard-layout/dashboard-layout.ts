import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CompanyStore } from '../../../Services/company-store';
import { AuthStore } from '../../../Services/auth-store';
import { CompanyPicker } from '../company-picker/company-picker';
import { EditCompanyDialog } from '../../Company/edit-company-dialog/edit-company-dialog';

@Component({
  selector: 'app-dashboard-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ButtonModule,
    CompanyPicker,
    EditCompanyDialog,
  ],
  host: { class: 'block' },
  styleUrl: './dashboard-layout.css',
  templateUrl: './dashboard-layout.html',
})
export class DashboardLayout {
  companyStore = inject(CompanyStore);
  authStore = inject(AuthStore);
  router = inject(Router);

  sidebarOpen = signal(false);
  editCompanyVisible = signal(false);

  navItems = [
    { label: 'Dashboard', icon: 'pi pi-home', route: '/dashboard' },
    { label: 'Clients', icon: 'pi pi-users', route: '/dashboard/clients' },
    { label: 'Products', icon: 'pi pi-box', route: '/dashboard/products' },
    { label: 'Invoices', icon: 'pi pi-file', route: '/dashboard/invoices' },
    { label: 'Estimates', icon: 'pi pi-file-edit', route: '/dashboard/estimates' },
    { label: 'Payments', icon: 'pi pi-wallet', route: '/dashboard/payments' },
  ];

  toggleSidebar() {
    this.sidebarOpen.update((v) => !v);
  }

  closeSidebar() {
    this.sidebarOpen.set(false);
  }

  switchCompany() {
    this.companyStore.clearCompany();
  }

  logout() {
    this.authStore.clearToken();
    this.companyStore.clearCompany();
    this.router.navigate(['/login']);
  }
}
