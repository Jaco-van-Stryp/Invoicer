import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { InvoiceService, GetPublicInvoiceResponse } from '../../../api';
import { ButtonModule } from 'primeng/button';
import { DatePipe, CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-public-invoice-view',
  imports: [ButtonModule, DatePipe, CurrencyPipe],
  templateUrl: './public-invoice-view.html',
  styleUrl: './public-invoice-view.css',
})
export class PublicInvoiceView implements OnInit {
  route = inject(ActivatedRoute);
  invoiceService = inject(InvoiceService);

  invoice = signal<GetPublicInvoiceResponse | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit() {
    const invoiceId = this.route.snapshot.paramMap.get('id');
    if (!invoiceId) {
      this.error.set('Invalid invoice link');
      this.loading.set(false);
      return;
    }

    this.invoiceService.getPublicInvoice(invoiceId).subscribe({
      next: (data) => {
        this.invoice.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Invoice not found');
        this.loading.set(false);
      },
    });
  }

  print() {
    window.print();
  }

  get subtotal(): number {
    return this.invoice()?.products?.reduce((sum, p) => sum + (p.totalPrice ?? 0), 0) ?? 0;
  }

  get total(): number {
    return this.invoice()?.totalAmount ?? 0;
  }
}
