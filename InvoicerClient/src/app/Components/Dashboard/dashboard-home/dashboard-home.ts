import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import {
  ClientService,
  GetAllClientsResponse,
  GetAllProductsResponse,
  GetAllInvoicesResponse,
  ProductService,
  InvoiceService,
} from '../../../api';
import { CompanyStore } from '../../../Services/company-store';

@Component({
  selector: 'app-dashboard-home',
  imports: [RouterLink, ButtonModule],
  host: { class: 'block' },
  styleUrl: './dashboard-home.css',
  templateUrl: './dashboard-home.html',
})
export class DashboardHome implements OnInit {
  companyStore = inject(CompanyStore);
  clientService = inject(ClientService);
  productService = inject(ProductService);
  invoiceService = inject(InvoiceService);
  router = inject(Router);

  clients = signal<GetAllClientsResponse[]>([]);
  products = signal<GetAllProductsResponse[]>([]);
  invoices = signal<GetAllInvoicesResponse[]>([]);
  loading = signal(true);

  company = this.companyStore.company;
  clientCount = computed(() => this.clients().length);
  productCount = computed(() => this.products().length);
  invoiceCount = computed(() => this.invoices().length);

  ngOnInit() {
    this.loadStats();
  }

  loadStats() {
    const companyId = this.company()?.id;
    if (!companyId) return;

    this.loading.set(true);
    let completed = 0;
    const checkDone = () => {
      completed++;
      if (completed >= 3) this.loading.set(false);
    };

    this.clientService.getAllClients(companyId).subscribe({
      next: (r) => this.clients.set(r),
      error: () => checkDone(),
      complete: () => checkDone(),
    });

    this.productService.getAllProducts(companyId).subscribe({
      next: (r) => this.products.set(r),
      error: () => checkDone(),
      complete: () => checkDone(),
    });

    this.invoiceService.getAllInvoices(companyId).subscribe({
      next: (r) => this.invoices.set(r),
      error: () => checkDone(),
      complete: () => checkDone(),
    });
  }
}
