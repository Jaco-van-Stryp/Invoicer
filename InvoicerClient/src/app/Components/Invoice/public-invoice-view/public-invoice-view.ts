import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  PLATFORM_ID,
  signal,
} from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { InvoiceService, GetPublicInvoiceResponse } from '../../../api';
import { ButtonModule } from 'primeng/button';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FileUrlPipe } from '../../../Pipes/file-url.pipe';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-public-invoice-view',
  imports: [ButtonModule, DatePipe, CurrencyPipe, FileUrlPipe],
  host: { class: 'block' },
  templateUrl: './public-invoice-view.html',
  styleUrl: './public-invoice-view.css',
})
export class PublicInvoiceView implements OnInit {
  route = inject(ActivatedRoute);
  invoiceService = inject(InvoiceService);
  private platformId = inject(PLATFORM_ID);
  private document = inject(DOCUMENT);

  invoice = signal<GetPublicInvoiceResponse | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  printUrl = signal('');

  subtotal = computed(
    () => this.invoice()?.products?.reduce((sum, p) => sum + (p.totalPrice ?? 0), 0) ?? 0,
  );

  total = computed(() => this.invoice()?.totalAmount ?? 0);

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.printUrl.set(this.document.location.href);
    }

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
    if (isPlatformBrowser(this.platformId)) {
      const inv = this.invoice();
      const originalTitle = this.document.title;
      if (inv?.company?.name && inv?.invoiceNumber) {
        this.document.title = `${inv.company.name} - ${inv.invoiceNumber}`;
      }
      window.print();
      this.document.title = originalTitle;
    }
  }
}
