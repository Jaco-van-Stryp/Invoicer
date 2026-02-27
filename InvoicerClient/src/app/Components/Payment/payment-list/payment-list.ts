import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { GetAllInvoicesResponse, GetAllPaymentsResponse, InvoiceService, PaymentService } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { PaymentFormDialog } from '../payment-form-dialog/payment-form-dialog';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-payment-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    SelectModule,
    TooltipModule,
    PaymentFormDialog,
  ],
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

  // Filter state
  searchText = signal('');
  filterMonth = signal<number | null>(null);
  filterYear = signal<number | null>(null);

  availableYears = computed(() => {
    const years = new Set<number>();
    for (const p of this.payments()) {
      if (p.paidOn) years.add(new Date(p.paidOn).getFullYear());
    }
    return [...years].sort((a, b) => b - a).map(y => ({ label: String(y), value: y }));
  });

  availableMonths = computed(() => {
    const year = this.filterYear();
    const months = new Set<number>();
    for (const p of this.payments()) {
      if (!p.paidOn) continue;
      const d = new Date(p.paidOn);
      if (year !== null && d.getFullYear() !== year) continue;
      months.add(d.getMonth());
    }
    return [...months].sort((a, b) => a - b).map(m => ({ label: MONTH_NAMES[m], value: m }));
  });

  filteredPayments = computed(() => {
    const search = this.searchText().toLowerCase().trim();
    const month = this.filterMonth();
    const year = this.filterYear();

    return this.payments().filter(p => {
      if (
        search &&
        !p.invoiceNumber?.toLowerCase().includes(search) &&
        !p.clientName?.toLowerCase().includes(search)
      ) {
        return false;
      }
      if (month !== null || year !== null) {
        const d = p.paidOn ? new Date(p.paidOn) : null;
        if (!d) return false;
        if (month !== null && d.getMonth() !== month) return false;
        if (year !== null && d.getFullYear() !== year) return false;
      }
      return true;
    });
  });

  hasActiveFilters = computed(
    () =>
      this.searchText().trim().length > 0 ||
      this.filterMonth() !== null ||
      this.filterYear() !== null,
  );

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
      error: () => {
        this.invoices.set([]);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load invoices.',
        });
      },
    });
  }

  onYearChange(year: number | null) {
    this.filterYear.set(year);
    this.filterMonth.set(null);
  }

  clearFilters() {
    this.searchText.set('');
    this.filterMonth.set(null);
    this.filterYear.set(null);
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
