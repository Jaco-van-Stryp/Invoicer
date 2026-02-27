import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { GetAllInvoicesResponse, PaymentService } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-payment-form-dialog',
  imports: [
    CurrencyPipe,
    FormsModule,
    DialogModule,
    ButtonModule,
    InputNumberModule,
    DatePickerModule,
    InputTextModule,
    SelectModule,
  ],
  host: { class: 'block' },
  styleUrl: './payment-form-dialog.css',
  templateUrl: './payment-form-dialog.html',
})
export class PaymentFormDialog {
  paymentService = inject(PaymentService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  visible = model(false);
  invoices = input<GetAllInvoicesResponse[]>([]);
  saved = output<void>();

  selectedInvoiceId = signal<string | null>(null);
  amount = signal<number | null>(null);
  paidOn = signal<Date | null>(new Date());
  notes = signal('');
  saving = signal(false);

  invoiceOptions = computed(() =>
    this.invoices().map((inv) => ({
      label: `${inv.invoiceNumber} — ${inv.clientName ?? ''}`,
      value: inv.id,
    })),
  );

  selectedInvoice = computed(
    () => this.invoices().find((inv) => inv.id === this.selectedInvoiceId()) ?? null,
  );

  outstanding = computed(() => {
    const inv = this.selectedInvoice();
    if (!inv) return null;
    return (inv.totalDue ?? 0) - (inv.totalPaid ?? 0);
  });

  isFormValid = computed(
    () =>
      this.selectedInvoiceId() !== null && (this.amount() ?? 0) > 0 && this.paidOn() !== null,
  );

  save() {
    const inv = this.selectedInvoice();
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
            detail: 'Payment recorded successfully.',
          });
          this.reset();
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

  cancel() {
    this.reset();
    this.visible.set(false);
  }

  private reset() {
    this.selectedInvoiceId.set(null);
    this.amount.set(null);
    this.paidOn.set(new Date());
    this.notes.set('');
  }
}
