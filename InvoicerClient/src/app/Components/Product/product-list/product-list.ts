import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { Table, TableModule } from 'primeng/table';
import { ProductService, GetAllProductsResponse } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { ProductFormDialog } from '../product-form-dialog/product-form-dialog';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-product-list',
  imports: [
    CurrencyPipe,
    TableModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    ConfirmDialogModule,
    ProductFormDialog,
  ],
  host: { class: 'block' },
  styleUrl: './product-list.css',
  templateUrl: './product-list.html',
})
export class ProductList implements OnInit {
  productService = inject(ProductService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);
  confirmationService = inject(ConfirmationService);

  products = signal<GetAllProductsResponse[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  selectedProduct = signal<GetAllProductsResponse | null>(null);

  dt = viewChild<Table>('dt');

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loading.set(true);
    this.productService.getAllProducts(companyId).subscribe({
      next: (r) => this.products.set(r),
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load products.',
        });
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  openNew() {
    this.selectedProduct.set(null);
    this.dialogVisible.set(true);
  }

  editProduct(product: GetAllProductsResponse) {
    this.selectedProduct.set(product);
    this.dialogVisible.set(true);
  }

  deleteProduct(product: GetAllProductsResponse) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${product.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        const companyId = this.companyStore.company()?.id;
        if (!companyId || !product.id) return;

        this.productService.deleteProduct(companyId, product.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Deleted',
              detail: 'Product deleted successfully.',
            });
            this.loadProducts();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete product.',
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

  onSaved() {
    this.loadProducts();
  }
}
