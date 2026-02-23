import { Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { AbstractControl, FormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ClientService, GetAllClientsResponse } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-client-form-dialog',
  imports: [
    FormsModule,
    DialogModule,
    ButtonModule,
    FloatLabelModule,
    InputTextModule,
    TextareaModule,
  ],
  host: { class: 'block' },
  templateUrl: './client-form-dialog.html',
})
export class ClientFormDialog {
  clientService = inject(ClientService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  visible = model(false);
  client = input<GetAllClientsResponse | null>(null);
  saved = output<void>();

  name = signal('');
  email = signal('');
  address = signal('');
  taxNumber = signal('');
  phoneNumber = signal('');
  saving = signal(false);

  isEditing = computed(() => !!this.client()?.id);
  dialogTitle = computed(() => (this.isEditing() ? 'Edit Client' : 'New Client'));

  isFormValid = computed(() => {
    const n = this.name().trim();
    const e = this.email().trim();
    if (!n || !e) return false;
    return Validators.email({ value: e } as AbstractControl) === null;
  });

  constructor() {
    effect(() => {
      const c = this.client();
      if (c) {
        this.name.set(c.name ?? '');
        this.email.set(c.email ?? '');
        this.address.set(c.address ?? '');
        this.taxNumber.set(c.taxNumber ?? '');
        this.phoneNumber.set(c.phoneNumber ?? '');
      } else {
        this.name.set('');
        this.email.set('');
        this.address.set('');
        this.taxNumber.set('');
        this.phoneNumber.set('');
      }
    });
  }

  save() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.saving.set(true);

    if (this.isEditing()) {
      this.clientService
        .updateClient({
          companyId,
          clientId: this.client()!.id,
          name: this.name(),
          email: this.email(),
          address: this.address() || null,
          taxNumber: this.taxNumber() || null,
          phoneNumber: this.phoneNumber() || null,
        })
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Updated',
              detail: 'Client updated successfully.',
            });
            this.visible.set(false);
            this.saved.emit();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to update client.',
            });
          },
          complete: () => this.saving.set(false),
        });
    } else {
      this.clientService
        .createClient({
          companyId,
          name: this.name(),
          email: this.email(),
          address: this.address() || undefined,
          taxNumber: this.taxNumber() || undefined,
          phoneNumber: this.phoneNumber() || undefined,
        })
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Created',
              detail: 'Client created successfully.',
            });
            this.visible.set(false);
            this.saved.emit();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to create client.',
            });
          },
          complete: () => this.saving.set(false),
        });
    }
  }
}
