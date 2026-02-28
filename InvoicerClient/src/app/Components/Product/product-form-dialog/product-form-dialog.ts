import { Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { FileUploadModule } from 'primeng/fileupload';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ProductService, GetAllProductsResponse, FileService } from '../../../api';
import { FileUrlPipe } from '../../../Pipes/file-url.pipe';
import { CompanyStore } from '../../../Services/company-store';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-product-form-dialog',
  imports: [
    FormsModule,
    DialogModule,
    ButtonModule,
    FileUploadModule,
    FloatLabelModule,
    InputNumberModule,
    InputTextModule,
    TextareaModule,
    FileUrlPipe,
  ],
  host: { class: 'block' },
  styleUrl: './product-form-dialog.css',
  templateUrl: './product-form-dialog.html',
})
export class ProductFormDialog {
  productService = inject(ProductService);
  fileService = inject(FileService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  visible = model(false);
  product = input<GetAllProductsResponse | null>(null);
  saved = output<void>();

  name = signal('');
  description = signal('');
  price = signal<number | null>(null);
  imageUrl = signal('');
  imagePreview = signal<string | null>(null);
  uploadingImage = signal(false);
  saving = signal(false);

  isEditing = computed(() => !!this.product()?.id);
  dialogTitle = computed(() => (this.isEditing() ? 'Edit Product' : 'New Product'));

  isFormValid = computed(() => {
    return (
      !!this.name().trim() &&
      !!this.description().trim() &&
      this.price() !== null &&
      this.price()! >= 0 &&
      !this.uploadingImage()
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
        this.imagePreview.set(p.imageUrl ?? null);
      } else {
        this.name.set('');
        this.description.set('');
        this.price.set(null);
        this.imageUrl.set('');
        this.imagePreview.set(null);
        this.uploadingImage.set(false);
        this.saving.set(false);
      }
    });
  }

  onImageSelected(event: { files: File[] }) {
    const file = event.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => this.imagePreview.set(reader.result as string);
    reader.readAsDataURL(file);

    this.uploadingImage.set(true);
    this.fileService.uploadFile(file).subscribe({
      next: (url) => {
        this.imageUrl.set(url);
        this.messageService.add({
          severity: 'success',
          summary: 'Image Uploaded',
          detail: 'Product image uploaded successfully.',
        });
      },
      error: () => {
        this.uploadingImage.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Upload Failed',
          detail: 'Failed to upload image. Please try again.',
        });
        this.imagePreview.set(this.product()?.imageUrl ?? null);
      },
      complete: () => this.uploadingImage.set(false),
    });
  }

  removeImage() {
    this.imageUrl.set('');
    this.imagePreview.set(null);
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
            this.saving.set(false);
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
            this.saving.set(false);
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
