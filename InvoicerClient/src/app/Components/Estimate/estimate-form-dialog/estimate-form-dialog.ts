import { Component, input, model, output, inject, signal, effect, computed } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-estimate-form-dialog',
  imports: [
    ReactiveFormsModule,
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
  loading = signal(false);

  isEditing = computed(() => !!this.estimate()?.id);

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
    products: new FormArray<FormGroup>([]),
  });

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.loadData();
        this.resetForm();
      }
    });
  }

  get productsFormArray() {
    return this.form.get('products') as FormArray;
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

    this.productsFormArray.clear();

    if (est) {
      this.form.patchValue({
        clientId: est.clientId,
        estimateDate: est.estimateDate ? new Date(est.estimateDate) : new Date(),
        expiresOn: est.expiresOn ? new Date(est.expiresOn) : new Date(),
        status: est.status,
        notes: est.notes || '',
      });

      est.products?.forEach((p) => {
        this.productsFormArray.push(
          new FormGroup({
            productId: new FormControl(p.productId, Validators.required),
            quantity: new FormControl(p.quantity, [Validators.required, Validators.min(1)]),
          }),
        );
      });
    } else {
      this.form.reset({
        estimateDate: new Date(),
        expiresOn: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
        status: EstimateStatus.Draft,
      });
    }
  }

  addProduct() {
    this.productsFormArray.push(
      new FormGroup({
        productId: new FormControl<string | null>(null, Validators.required),
        quantity: new FormControl<number>(1, [Validators.required, Validators.min(1)]),
      }),
    );
  }

  removeProduct(index: number) {
    this.productsFormArray.removeAt(index);
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
    const products = formValue.products!.map((p) => ({
      productId: p.productId!,
      quantity: p.quantity!,
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
