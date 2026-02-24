import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { GetAllPaymentsResponse, PaymentService } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-payment-list',
  imports: [CurrencyPipe, DatePipe, ButtonModule, ConfirmDialogModule, TableModule, TooltipModule],
  host: { class: 'block' },
  styleUrl: './payment-list.css',
  templateUrl: './payment-list.html',
})
export class PaymentList implements OnInit {
  paymentService = inject(PaymentService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);
  confirmationService = inject(ConfirmationService);

  payments = signal<GetAllPaymentsResponse[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.loadPayments();
  }

  loadPayments() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loading.set(true);
    this.paymentService.getAllPayments(companyId).subscribe({
      next: (r) => this.payments.set(r),
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load payments.',
        });
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  deletePayment(payment: GetAllPaymentsResponse) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete this payment?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        if (!payment.id || !payment.invoiceId) return;
        const companyId = this.companyStore.company()?.id;
        if (!companyId) return;

        this.paymentService.deletePayment(companyId, payment.invoiceId, payment.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Deleted',
              detail: 'Payment deleted successfully.',
            });
            this.loadPayments();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete payment.',
            });
          },
        });
      },
    });
  }
}
