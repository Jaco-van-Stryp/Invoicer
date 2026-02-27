import { Component, input, model, output, inject, signal, effect, computed, ChangeDetectionStrategy } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import {
  EstimateService,
  ClientService,
  ProductService,
  GetAllEstimatesResponse,
  GetAllClientsResponse,
  GetAllProductsResponse,
  EstimateStatus,
  CreateEstimateCommand,
  UpdateEstimateCommand,
} from '../../../api';
import { CompanyStore } from '../../../Services/company-store';

interface ProductLine {
  productId: string;
  quantity: number;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-estimate-form-dialog',
  imports: [
    ReactiveFormsModule,
    FormsModule,
    CurrencyPipe,
    DialogModule,
    ButtonModule,
    InputTextModule,
    DatePickerModule,
    SelectModule,
    InputNumberModule,
  ],
  host: { class: 'block' },
  templateUrl: './estimate-form-dialog.html',
  styleUrl: './estimate-form-dialog.css',
})
export class EstimateFormDialog {
  visible = model.required<boolean>();
  estimate = input<GetAllEstimatesResponse | null>(null);
  saved = output<void>();

  estimateService = inject(EstimateService);
  clientService = inject(ClientService);
  productService = inject(ProductService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  clients = signal<GetAllClientsResponse[]>([]);
  products = signal<GetAllProductsResponse[]>([]);
  productLines = signal<ProductLine[]>([]);
  loading = signal(false);

  isEditing = computed(() => !!this.estimate()?.id);

  grandTotal = computed(() => {
    const lines = this.productLines();
    const prods = this.products();
    return lines.reduce((sum, line) => {
      const product = prods.find((p) => p.id === line.productId);
      return sum + (product?.price ?? 0) * line.quantity;
    }, 0);
  });

  statusOptions = Object.values(EstimateStatus).map((status) => ({
    label: status,
    value: status,
  }));

  form = new FormGroup({
    clientId: new FormControl<string | null>(null, Validators.required),
    estimateDate: new FormControl<Date>(new Date(), Validators.required),
    expiresOn: new FormControl<Date>(
      new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
      Validators.required,
    ),
    status: new FormControl<EstimateStatus>(EstimateStatus.Draft, Validators.required),
    notes: new FormControl<string>(''),
  });

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.loadData();
        this.resetForm();
      }
    });
  }

  loadData() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.clientService.getAllClients(companyId).subscribe({
      next: (r) => this.clients.set(r),
    });

    this.productService.getAllProducts(companyId).subscribe({
      next: (r) => this.products.set(r),
    });
  }

  resetForm() {
    const est = this.estimate();
    this.productLines.set([]);

    if (est) {
      this.form.patchValue({
        clientId: est.clientId,
        estimateDate: est.estimateDate ? new Date(est.estimateDate) : new Date(),
        expiresOn: est.expiresOn ? new Date(est.expiresOn) : new Date(),
        status: est.status,
        notes: est.notes || '',
      });

      this.productLines.set(
        (est.products ?? []).map((p) => ({
          productId: p.productId ?? '',
          quantity: p.quantity ?? 1,
        })),
      );
    } else {
      this.form.reset({
        estimateDate: new Date(),
        expiresOn: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
        status: EstimateStatus.Draft,
      });
    }
  }

  addProduct() {
    this.productLines.update((lines) => [...lines, { productId: '', quantity: 1 }]);
  }

  removeProduct(index: number) {
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
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loading.set(true);

    const formValue = this.form.value;
    const products = this.productLines().map((p) => ({
      productId: p.productId,
      quantity: p.quantity,
    }));

    if (this.isEditing()) {
      const command: UpdateEstimateCommand = {
        companyId,
        estimateId: this.estimate()!.id,
        clientId: formValue.clientId!,
        estimateDate: this.formatDate(formValue.estimateDate!),
        expiresOn: this.formatDate(formValue.expiresOn!),
        status: formValue.status!,
        notes: formValue.notes || undefined,
        products,
      };

      this.estimateService.updateEstimate(command).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Updated',
            detail: 'Estimate updated successfully',
          });
          this.visible.set(false);
          this.saved.emit();
          this.loading.set(false);
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update estimate',
          });
          this.loading.set(false);
        },
      });
    } else {
      const command: CreateEstimateCommand = {
        companyId,
        clientId: formValue.clientId!,
        estimateDate: this.formatDate(formValue.estimateDate!),
        expiresOn: this.formatDate(formValue.expiresOn!),
        status: formValue.status!,
        notes: formValue.notes || undefined,
        products,
      };

      this.estimateService.createEstimate(command).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Created',
            detail: 'Estimate created successfully',
          });
          this.visible.set(false);
          this.saved.emit();
          this.loading.set(false);
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to create estimate',
          });
          this.loading.set(false);
        },
      });
    }
  }

  onCancel() {
    this.visible.set(false);
  }
}
