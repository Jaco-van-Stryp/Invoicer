import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { HttpClient } from '@angular/common/http';
import { CompanyStore } from '../../../Services/company-store';

interface GetAllPaymentsResponse {
  id: string;
  amount: number;
  paidOn: string;
  notes?: string;
  invoiceId: string;
  invoiceNumber: string;
  clientName: string;
}

@Component({
  selector: 'app-payment-list',
  imports: [CommonModule, ButtonModule, TableModule, TooltipModule],
  host: { class: 'block' },
  styleUrl: './payment-list.css',
  templateUrl: './payment-list.html',
})
export class PaymentList implements OnInit {
  companyStore = inject(CompanyStore);
  http = inject(HttpClient);

  payments = signal<GetAllPaymentsResponse[]>([]);
  loading = signal(true);

  private apiUrl = 'https://localhost:7261/api';

  ngOnInit() {
    this.loadPayments();
  }

  loadPayments() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loading.set(true);
    this.http
      .get<GetAllPaymentsResponse[]>(`${this.apiUrl}/payment/all-payments?companyId=${companyId}`)
      .subscribe({
        next: (r) => this.payments.set(r),
        error: (err) => {
          console.error('Failed to load payments', err);
          this.loading.set(false);
        },
        complete: () => this.loading.set(false),
      });
  }

  deletePayment(id: string) {
    if (!confirm('Are you sure you want to delete this payment?')) return;

    this.http.delete(`${this.apiUrl}/payment/delete-payment?paymentId=${id}`).subscribe({
      next: () => this.loadPayments(),
      error: (err) => console.error('Failed to delete payment', err),
    });
  }
}
