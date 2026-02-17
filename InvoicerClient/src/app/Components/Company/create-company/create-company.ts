import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CompanyService, CreateCompanyCommand } from '../../../api';
import { Copyright } from '../../General/copyright/copyright';

@Component({
  selector: 'app-create-company',
  imports: [
    FormsModule,
    ButtonModule,
    FloatLabelModule,
    InputTextModule,
    TextareaModule,
    Copyright,
  ],
  host: { class: 'block' },
  styleUrl: './create-company.css',
  templateUrl: './create-company.html',
})
export class CreateCompany {
  router = inject(Router);
  companyService = inject(CompanyService);
  messageService = inject(MessageService);

  loading = signal(false);
  name = signal('');
  address = signal('');
  taxNumber = signal('');
  phoneNumber = signal('');
  email = signal('');
  paymentDetails = signal('');

  isFormValid = computed(() => this.name().trim().length > 0);

  createCompany() {
    this.loading.set(true);
    const command: CreateCompanyCommand = {
      name: this.name(),
      address: this.address() || undefined,
      taxNumber: this.taxNumber() || undefined,
      phoneNumber: this.phoneNumber() || undefined,
      email: this.email() || undefined,
      paymentDetails: this.paymentDetails() || undefined,
    };
    this.companyService.createCompany(command).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Company Created',
          detail: 'Your company has been set up successfully',
        });
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to create company. Please try again.',
        });
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }
}
