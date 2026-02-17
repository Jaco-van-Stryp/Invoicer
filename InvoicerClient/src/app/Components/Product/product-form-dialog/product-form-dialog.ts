import { Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ProductService, GetAllProductsResponse } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';

@Component({
  selector: 'app-product-form-dialog',
  imports: [
    FormsModule,
    DialogModule,
    ButtonModule,
    FloatLabelModule,
    InputNumberModule,
    InputTextModule,
    TextareaModule,
  ],
  host: { class: 'block' },
  templateUrl: './product-form-dialog.html',
})
export class ProductFormDialog {
  productService = inject(ProductService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  visible = model(false);
  product = input<GetAllProductsResponse | null>(null);
  saved = output<void>();

  name = signal('');
  description = signal('');
  price = signal<number | null>(null);
  imageUrl = signal('');
  saving = signal(false);

  isEditing = computed(() => !!this.product()?.id);
  dialogTitle = computed(() => (this.isEditing() ? 'Edit Product' : 'New Product'));

  isFormValid = computed(() => {
    return (
      !!this.name().trim() &&
      !!this.description().trim() &&
      this.price() !== null &&
      this.price()! >= 0
    );
  });

  constructor() {
    effect(() => {
      const p = this.product();
      if (p) {
        this.name.set(p.name ?? '');
        this.description.set(p.description ?? '');
        this.price.set(p.price ?? null);
        this.imageUrl.set(p.imageUrl ?? '');
      } else {
        this.name.set('');
        this.description.set('');
        this.price.set(null);
        this.imageUrl.set('');
      }
    });
  }

  save() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.saving.set(true);

    if (this.isEditing()) {
      this.productService
        .updateProduct({
          companyId,
          productId: this.product()!.id,
          name: this.name(),
          description: this.description(),
          price: this.price(),
          imageUrl: this.imageUrl() || null,
        })
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Updated',
              detail: 'Product updated successfully.',
            });
            this.visible.set(false);
            this.saved.emit();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to update product.',
            });
          },
          complete: () => this.saving.set(false),
        });
    } else {
      this.productService
        .createProduct({
          companyId,
          name: this.name(),
          description: this.description(),
          price: this.price() ?? 0,
          imageUrl: this.imageUrl() || null,
        })
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Created',
              detail: 'Product created successfully.',
            });
            this.visible.set(false);
            this.saved.emit();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to create product.',
            });
          },
          complete: () => this.saving.set(false),
        });
    }
  }
}
