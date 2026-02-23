import { Component, inject, OnInit, signal, computed } from '@angular/core';
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
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-dashboard-home',
  imports: [RouterLink, ButtonModule, ChartModule],
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

    // Get last 12 months including empty months
    const now = new Date();
    const monthsData: { year: number; month: number; amount: number }[] = [];

    for (let i = 11; i >= 0; i--) {
      const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const year = date.getFullYear();
      const month = date.getMonth() + 1;

      const existing = stats.monthlyIncome.find((m) => m.year === year && m.month === month);
      monthsData.push({
        year,
        month,
        amount: existing?.totalPaid ?? 0,
      });
    }

    return {
      labels: monthsData.map((m) => {
        const monthNames = [
          'Jan',
          'Feb',
          'Mar',
          'Apr',
          'May',
          'Jun',
          'Jul',
          'Aug',
          'Sep',
          'Oct',
          'Nov',
          'Dec',
        ];
        return `${monthNames[m.month - 1]} ${m.year}`;
      }),
      datasets: [
        {
          label: 'Income',
          data: monthsData.map((m) => m.amount),
          borderColor: '#6366f1',
          backgroundColor: 'rgba(99, 102, 241, 0.1)',
          fill: true,
          tension: 0.4,
          pointRadius: 3,
          pointHoverRadius: 6,
          pointBackgroundColor: '#6366f1',
          pointBorderColor: '#fff',
          pointBorderWidth: 2,
        },
      ],
    };
  });

  lineChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        mode: 'index' as const,
        intersect: false,
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          color: 'rgba(255, 255, 255, 0.05)',
        },
        ticks: {
          color: 'rgba(255, 255, 255, 0.5)',
        },
      },
      x: {
        grid: {
          display: false,
        },
        ticks: {
          color: 'rgba(255, 255, 255, 0.5)',
        },
      },
    },
    interaction: {
      mode: 'nearest' as const,
      axis: 'x' as const,
      intersect: false,
    },
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
