import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { GetAllInvoicesResponse, GetAllPaymentsResponse, InvoiceService, PaymentService } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { PaymentFormDialog } from '../payment-form-dialog/payment-form-dialog';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-payment-list',
  imports: [CurrencyPipe, DatePipe, ButtonModule, ConfirmDialogModule, TooltipModule, PaymentFormDialog],
  host: { class: 'block' },
  styleUrl: './payment-list.css',
  templateUrl: './payment-list.html',
})
export class PaymentList implements OnInit {
  paymentService = inject(PaymentService);
  invoiceService = inject(InvoiceService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);
  confirmationService = inject(ConfirmationService);

  payments = signal<GetAllPaymentsResponse[]>([]);
  invoices = signal<GetAllInvoicesResponse[]>([]);
  loading = signal(true);
  recordDialogVisible = signal(false);

  ngOnInit() {
    this.loadPayments();
    this.loadInvoices();
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

  loadInvoices() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.invoiceService.getAllInvoices(companyId).subscribe({
      next: (r) => this.invoices.set(r),
      error: () => {},
    });
  }

  openRecordPayment() {
    this.recordDialogVisible.set(true);
  }

  onPaymentSaved() {
    this.loadPayments();
    this.loadInvoices();
  }

  deletePayment(payment: GetAllPaymentsResponse) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete this payment of ${payment.amount ? '$' + payment.amount.toFixed(2) : 'this amount'}?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { severity: 'danger', label: 'Yes, Delete' },
      rejectButtonProps: { severity: 'secondary', outlined: true, label: 'Cancel' },
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
