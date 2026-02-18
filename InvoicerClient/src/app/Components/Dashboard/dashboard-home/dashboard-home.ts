import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ChartModule } from 'primeng/chart';
import {
  ClientService,
  GetAllClientsResponse,
  GetAllProductsResponse,
  GetAllInvoicesResponse,
  GetDashboardStatsResponse,
  ProductService,
  InvoiceService,
} from '../../../api';
import { CompanyStore } from '../../../Services/company-store';

@Component({
  selector: 'app-dashboard-home',
  imports: [RouterLink, ButtonModule, ChartModule, CurrencyPipe, DatePipe],
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
  dashboardStats = signal<GetDashboardStatsResponse | null>(null);
  loading = signal(true);

  company = this.companyStore.company;
  clientCount = computed(() => this.clients().length);
  productCount = computed(() => this.products().length);
  invoiceCount = computed(() => this.invoices().length);

  monthlyChartData = computed(() => {
    const stats = this.dashboardStats();
    if (!stats?.monthlyIncome?.length) return null;
    return {
      labels: stats.monthlyIncome.map((m) => `${m.year}-${String(m.month).padStart(2, '0')}`),
      datasets: [
        {
          label: 'Income',
          data: stats.monthlyIncome.map((m) => m.totalPaid ?? 0),
          backgroundColor: '#6366f1',
          borderRadius: 4,
        },
      ],
    };
  });

  clientChartData = computed(() => {
    const stats = this.dashboardStats();
    if (!stats?.incomeByClient?.length) return null;
    const colors = ['#6366f1', '#a855f7', '#ec4899', '#3b82f6', '#22c55e', '#f59e0b'];
    return {
      labels: stats.incomeByClient.map((c) => c.clientName ?? 'Unknown'),
      datasets: [
        {
          data: stats.incomeByClient.map((c) => c.totalPaid ?? 0),
          backgroundColor: stats.incomeByClient.map((_, i) => colors[i % colors.length]),
        },
      ],
    };
  });

  statusChartData = computed(() => {
    const stats = this.dashboardStats();
    if (!stats?.statusSummary) return null;
    const s = stats.statusSummary;
    return {
      labels: ['Paid', 'Partial', 'Unpaid'],
      datasets: [
        {
          data: [s.paidCount ?? 0, s.partialCount ?? 0, s.unpaidCount ?? 0],
          backgroundColor: ['#22c55e', '#f59e0b', '#ef4444'],
        },
      ],
    };
  });

  recentPayments = computed(() => {
    return this.invoices()
      .flatMap((inv) =>
        (inv.payments ?? []).map((p) => ({
          invoiceNumber: inv.invoiceNumber ?? '',
          clientName: inv.clientName ?? '',
          amount: p.amount ?? 0,
          paidOn: p.paidOn ? new Date(p.paidOn) : null,
          notes: p.notes,
        })),
      )
      .sort((a, b) => (b.paidOn?.getTime() ?? 0) - (a.paidOn?.getTime() ?? 0))
      .slice(0, 10);
  });

  barChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true } },
  };

  doughnutChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' as const } },
  };

  pieChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' as const } },
  };

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
      if (completed >= 4) this.loading.set(false);
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

    this.invoiceService.getDashboardStats(companyId).subscribe({
      next: (r) => this.dashboardStats.set(r),
      error: () => checkDone(),
      complete: () => checkDone(),
    });
  }
}
