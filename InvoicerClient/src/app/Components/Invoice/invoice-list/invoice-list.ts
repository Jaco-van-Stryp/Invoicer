import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { InvoiceService, GetAllInvoicesResponse, InvoiceStatus } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { InvoiceFormDialog } from '../invoice-form-dialog/invoice-form-dialog';
import { RecordPaymentDialog } from '../record-payment-dialog/record-payment-dialog';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-invoice-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    SelectModule,
    TagModule,
    TooltipModule,
    InvoiceFormDialog,
    RecordPaymentDialog,
  ],
  host: { class: 'block' },
  styleUrl: './invoice-list.css',
  templateUrl: './invoice-list.html',
})
export class InvoiceList implements OnInit {
  router = inject(Router);
  invoiceService = inject(InvoiceService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);
  confirmationService = inject(ConfirmationService);

  invoices = signal<GetAllInvoicesResponse[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  selectedInvoice = signal<GetAllInvoicesResponse | null>(null);
  paymentDialogVisible = signal(false);
  selectedInvoiceForPayment = signal<GetAllInvoicesResponse | null>(null);

  // Filter state
  searchText = signal('');
  selectedStatuses = signal<InvoiceStatus[]>([]);
  filterMonth = signal<number | null>(null);
  filterYear = signal<number | null>(null);

  readonly statusOptions: { label: string; value: InvoiceStatus }[] = [
    { label: 'Unpaid', value: InvoiceStatus.Unpaid },
    { label: 'Partial', value: InvoiceStatus.Partial },
    { label: 'Paid', value: InvoiceStatus.Paid },
  ];

  availableYears = computed(() => {
    const years = new Set<number>();
    for (const inv of this.invoices()) {
      if (inv.invoiceDate) years.add(new Date(inv.invoiceDate).getFullYear());
    }
    return [...years].sort((a, b) => b - a).map(y => ({ label: String(y), value: y }));
  });

  availableMonths = computed(() => {
    const year = this.filterYear();
    const months = new Set<number>();
    for (const inv of this.invoices()) {
      if (!inv.invoiceDate) continue;
      const d = new Date(inv.invoiceDate);
      if (year !== null && d.getFullYear() !== year) continue;
      months.add(d.getMonth());
    }
    return [...months].sort((a, b) => a - b).map(m => ({ label: MONTH_NAMES[m], value: m }));
  });

  filteredInvoices = computed(() => {
    const search = this.searchText().toLowerCase().trim();
    const statuses = this.selectedStatuses();
    const month = this.filterMonth();
    const year = this.filterYear();

    return this.invoices().filter(inv => {
      if (
        search &&
        !inv.invoiceNumber?.toLowerCase().includes(search) &&
        !inv.clientName?.toLowerCase().includes(search)
      ) {
        return false;
      }
      if (statuses.length > 0 && (!inv.status || !statuses.includes(inv.status))) {
        return false;
      }
      if (month !== null || year !== null) {
        const d = inv.invoiceDate ? new Date(inv.invoiceDate) : null;
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
      this.selectedStatuses().length > 0 ||
      this.filterMonth() !== null ||
      this.filterYear() !== null,
  );

  ngOnInit() {
    this.loadInvoices();
  }

  loadInvoices() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loading.set(true);
    this.invoiceService.getAllInvoices(companyId).subscribe({
      next: (r) => this.invoices.set(r),
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load invoices.',
        });
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  toggleStatus(status: InvoiceStatus) {
    const current = this.selectedStatuses();
    if (current.includes(status)) {
      this.selectedStatuses.set(current.filter(s => s !== status));
    } else {
      this.selectedStatuses.set([...current, status]);
    }
  }

  onYearChange(year: number | null) {
    this.filterYear.set(year);
    this.filterMonth.set(null);
  }

  clearFilters() {
    this.searchText.set('');
    this.selectedStatuses.set([]);
    this.filterMonth.set(null);
    this.filterYear.set(null);
  }

  statusSeverity(status: InvoiceStatus | undefined): 'success' | 'warn' | 'danger' {
    if (status === InvoiceStatus.Paid) return 'success';
    if (status === InvoiceStatus.Partial) return 'warn';
    return 'danger';
  }

  openNew() {
    this.selectedInvoice.set(null);
    this.dialogVisible.set(true);
  }

  viewInvoice(invoice: GetAllInvoicesResponse) {
    this.router.navigate(['/invoice', invoice.id]);
  }

  editInvoice(invoice: GetAllInvoicesResponse) {
    this.selectedInvoice.set(invoice);
    this.dialogVisible.set(true);
  }

  openPaymentDialog(invoice: GetAllInvoicesResponse) {
    this.selectedInvoiceForPayment.set(invoice);
    this.paymentDialogVisible.set(true);
  }

  deleteInvoice(invoice: GetAllInvoicesResponse) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete invoice "${invoice.invoiceNumber}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { severity: 'danger', label: 'Yes, Delete' },
      rejectButtonProps: { severity: 'secondary', outlined: true, label: 'Cancel' },
      accept: () => {
        const companyId = this.companyStore.company()?.id;
        if (!companyId || !invoice.id) return;

        this.invoiceService.deleteInvoice(companyId, invoice.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Deleted',
              detail: 'Invoice deleted successfully.',
            });
            this.loadInvoices();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete invoice.',
            });
          },
        });
      },
    });
  }

  sendEmail(invoice: GetAllInvoicesResponse) {
    const companyId = this.companyStore.company()?.id;
    if (!companyId || !invoice.id) return;

    this.invoiceService.sendInvoiceEmail(invoice.id, { companyId }).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Email Sent',
          detail: `Invoice ${invoice.invoiceNumber} has been sent to ${invoice.clientName}.`,
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to send email.',
        });
      },
    });
  }

  onSaved() {
    this.loadInvoices();
  }
}
