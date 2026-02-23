import { Component, computed, inject, input, model, output, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { GetAllInvoicesResponse, InvoicePaymentItem, PaymentService } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-record-payment-dialog',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    DialogModule,
    ButtonModule,
    InputNumberModule,
    DatePickerModule,
    InputTextModule,
  ],
  styleUrl: './record-payment-dialog.css',
  templateUrl: './record-payment-dialog.html',
})
export class RecordPaymentDialog {
  paymentService = inject(PaymentService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  visible = model(false);
  invoice = input<GetAllInvoicesResponse | null>(null);
  saved = output<void>();

  amount = signal<number | null>(null);
  paidOn = signal<Date | null>(new Date());
  notes = signal('');
  saving = signal(false);
  deleting = signal<string | null>(null);

  isFormValid = computed(() => (this.amount() ?? 0) > 0 && this.paidOn() !== null);

  outstanding = computed(() => {
    const inv = this.invoice();
    if (!inv) return 0;
    return (inv.totalDue ?? 0) - (inv.totalPaid ?? 0);
  });

  save() {
    const inv = this.invoice();
    const companyId = this.companyStore.company()?.id;
    const amt = this.amount();
    const date = this.paidOn();
    if (!inv?.id || !companyId || !amt || !date) return;

    this.saving.set(true);
    this.paymentService
      .recordPayment({
        companyId,
        invoiceId: inv.id,
        amount: amt,
        paidOn: date.toISOString(),
        notes: this.notes() || undefined,
      })
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Payment Recorded',
            detail: 'Payment has been recorded successfully.',
          });
          this.amount.set(null);
          this.paidOn.set(new Date());
          this.notes.set('');
          this.saved.emit();
          this.visible.set(false);
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to record payment.',
          });
          this.saving.set(false);
        },
        complete: () => this.saving.set(false),
      });
  }

  deletePayment(payment: InvoicePaymentItem) {
    const inv = this.invoice();
    const companyId = this.companyStore.company()?.id;
    if (!inv?.id || !companyId || !payment.paymentId) return;

    this.deleting.set(payment.paymentId);
    this.paymentService.deletePayment(companyId, inv.id, payment.paymentId).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Payment Removed',
          detail: 'Payment has been removed.',
        });
        this.saved.emit();
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to remove payment.',
        });
        this.deleting.set(null);
      },
      complete: () => this.deleting.set(null),
    });
  }

  cancel() {
    this.amount.set(null);
    this.paidOn.set(new Date());
    this.notes.set('');
    this.visible.set(false);
  }
}
