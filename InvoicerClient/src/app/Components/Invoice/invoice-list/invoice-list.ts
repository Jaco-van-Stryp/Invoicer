import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { Table, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { InvoiceService, GetAllInvoicesResponse, InvoiceStatus } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { InvoiceFormDialog } from '../invoice-form-dialog/invoice-form-dialog';
import { RecordPaymentDialog } from '../record-payment-dialog/record-payment-dialog';

@Component({
  selector: 'app-invoice-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    TableModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    ConfirmDialogModule,
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
  viewMode = signal<'table' | 'cards'>('cards'); // Default to card view

  dt = viewChild<Table>('dt');

  toggleView() {
    this.viewMode.update((mode) => (mode === 'table' ? 'cards' : 'table'));
  }

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

  statusSeverity(status: InvoiceStatus | undefined): 'success' | 'warn' | 'danger' {
    if (status === InvoiceStatus.Paid) return 'success';
    if (status === InvoiceStatus.Partial) return 'warn';
    return 'danger';
  }

  openNew() {
    this.selectedInvoice.set(null);
    this.dialogVisible.set(true);
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

  onFilter(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.dt()?.filterGlobal(value, 'contains');
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
