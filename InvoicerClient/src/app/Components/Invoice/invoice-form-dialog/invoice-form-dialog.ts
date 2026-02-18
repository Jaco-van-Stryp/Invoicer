import { Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import {
  ClientService,
  GetAllClientsResponse,
  GetAllInvoicesResponse,
  GetAllProductsResponse,
  InvoiceService,
  ProductService,
} from '../../../api';
import { CompanyStore } from '../../../Services/company-store';

interface ProductLine {
  productId: string;
  quantity: number;
}

@Component({
  selector: 'app-invoice-form-dialog',
  imports: [
    CurrencyPipe,
    FormsModule,
    DialogModule,
    ButtonModule,
    FloatLabelModule,
    InputTextModule,
    InputNumberModule,
    DatePickerModule,
    SelectModule,
  ],
  host: { class: 'block' },
  styleUrl: './invoice-form-dialog.css',
  templateUrl: './invoice-form-dialog.html',
})
export class InvoiceFormDialog {
  invoiceService = inject(InvoiceService);
  clientService = inject(ClientService);
  productService = inject(ProductService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  visible = model(false);
  invoice = input<GetAllInvoicesResponse | null>(null);
  saved = output<void>();

  invoiceNumber = signal('');
  selectedClientId = signal<string | null>(null);
  invoiceDate = signal<Date | null>(null);
  invoiceDue = signal<Date | null>(null);
  productLines = signal<ProductLine[]>([]);
  saving = signal(false);

  clients = signal<GetAllClientsResponse[]>([]);
  products = signal<GetAllProductsResponse[]>([]);
  loadingData = signal(false);

  isEditing = computed(() => !!this.invoice()?.id);
  dialogTitle = computed(() => (this.isEditing() ? 'Edit Invoice' : 'New Invoice'));

  grandTotal = computed(() => {
    const lines = this.productLines();
    const prods = this.products();
    return lines.reduce((sum, line) => {
      const product = prods.find((p) => p.id === line.productId);
      return sum + (product?.price ?? 0) * line.quantity;
    }, 0);
  });

  isFormValid = computed(() => {
    return (
      !!this.selectedClientId() &&
      !!this.invoiceDate() &&
      !!this.invoiceDue() &&
      this.productLines().length > 0 &&
      this.productLines().every((l) => !!l.productId && l.quantity > 0)
    );
  });

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.loadDropdownData();
      }
    });

    effect(() => {
      const inv = this.invoice();
      if (inv) {
        this.invoiceNumber.set(inv.invoiceNumber ?? '');
        this.selectedClientId.set(inv.clientId ?? null);
        this.invoiceDate.set(inv.invoiceDate ? new Date(inv.invoiceDate) : null);
        this.invoiceDue.set(inv.invoiceDue ? new Date(inv.invoiceDue) : null);
        this.productLines.set(
          (inv.products ?? []).map((p) => ({
            productId: p.productId ?? '',
            quantity: p.quantity ?? 1,
          })),
        );
      } else {
        this.invoiceNumber.set('');
        this.selectedClientId.set(null);
        const today = new Date();
        const due = new Date();
        due.setDate(due.getDate() + 30);
        this.invoiceDate.set(today);
        this.invoiceDue.set(due);
        this.productLines.set([]);
      }
    });
  }

  loadDropdownData() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loadingData.set(true);
    let completed = 0;
    const checkDone = () => {
      completed++;
      if (completed >= 2) this.loadingData.set(false);
    };

    this.clientService.getAllClients(companyId).subscribe({
      next: (r) => this.clients.set(r),
      complete: () => checkDone(),
      error: () => checkDone(),
    });

    this.productService.getAllProducts(companyId).subscribe({
      next: (r) => this.products.set(r),
      complete: () => checkDone(),
      error: () => checkDone(),
    });
  }

  addLine() {
    this.productLines.update((lines) => [...lines, { productId: '', quantity: 1 }]);
  }

  removeLine(index: number) {
    this.productLines.update((lines) => lines.filter((_, i) => i !== index));
  }

  updateLineProduct(index: number, productId: string) {
    this.productLines.update((lines) =>
      lines.map((l, i) => (i === index ? { ...l, productId } : l)),
    );
  }

  updateLineQuantity(index: number, quantity: number) {
    this.productLines.update((lines) =>
      lines.map((l, i) => (i === index ? { ...l, quantity } : l)),
    );
  }

  getLineTotal(line: ProductLine): number {
    const product = this.products().find((p) => p.id === line.productId);
    return (product?.price ?? 0) * line.quantity;
  }

  private formatDate(date: Date): string {
    return date.toISOString().substring(0, 10);
  }

  save() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.saving.set(true);

    if (this.isEditing()) {
      this.invoiceService
        .updateInvoice({
          companyId,
          invoiceId: this.invoice()!.id,
          invoiceNumber: this.invoiceNumber(),
          clientId: this.selectedClientId(),
          invoiceDate: this.invoiceDate() ? this.formatDate(this.invoiceDate()!) : null,
          invoiceDue: this.invoiceDue() ? this.formatDate(this.invoiceDue()!) : null,
          products: this.productLines().map((l) => ({
            productId: l.productId,
            quantity: l.quantity,
          })),
        })
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Updated',
              detail: 'Invoice updated successfully.',
            });
            this.visible.set(false);
            this.saved.emit();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to update invoice.',
            });
          },
          complete: () => this.saving.set(false),
        });
    } else {
      this.invoiceService
        .createInvoice({
          companyId,
          clientId: this.selectedClientId()!,
          invoiceDate: this.invoiceDate() ? this.formatDate(this.invoiceDate()!) : undefined,
          invoiceDue: this.invoiceDue() ? this.formatDate(this.invoiceDue()!) : undefined,
          products: this.productLines().map((l) => ({
            productId: l.productId,
            quantity: l.quantity,
          })),
        })
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Created',
              detail: 'Invoice created successfully.',
            });
            this.visible.set(false);
            this.saved.emit();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to create invoice.',
            });
          },
          complete: () => this.saving.set(false),
        });
    }
  }
}
