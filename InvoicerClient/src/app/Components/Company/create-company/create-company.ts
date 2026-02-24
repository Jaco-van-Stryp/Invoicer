import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { FileUploadModule } from 'primeng/fileupload';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CompanyService, CreateCompanyCommand, FileService } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { Copyright } from '../../General/copyright/copyright';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-create-company',
  imports: [
    FormsModule,
    ButtonModule,
    FileUploadModule,
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
  companyStore = inject(CompanyStore);
  fileService = inject(FileService);
  messageService = inject(MessageService);

  loading = signal(false);
  name = signal('');
  address = signal('');
  taxNumber = signal('');
  phoneNumber = signal('');
  email = signal('');
  paymentDetails = signal('');
  logoUrl = signal('');
  logoPreview = signal<string | null>(null);
  uploadingLogo = signal(false);

  isFormValid = computed(() => this.name().trim().length > 0 && !this.uploadingLogo());

  onLogoSelected(event: { files: File[] }) {
    const file = event.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => this.logoPreview.set(reader.result as string);
    reader.readAsDataURL(file);

    this.uploadingLogo.set(true);
    this.fileService.uploadFile(file).subscribe({
      next: (filename) => {
        this.logoUrl.set(filename);
        this.messageService.add({
          severity: 'success',
          summary: 'Logo Uploaded',
          detail: 'Logo uploaded successfully.',
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Upload Failed',
          detail: 'Failed to upload logo. Please try again.',
        });
        this.logoPreview.set(null);
      },
      complete: () => this.uploadingLogo.set(false),
    });
  }

  removeLogo() {
    this.logoUrl.set('');
    this.logoPreview.set(null);
  }

  createCompany() {
    this.loading.set(true);
    const command: CreateCompanyCommand = {
      name: this.name(),
      address: this.address() || undefined,
      taxNumber: this.taxNumber() || undefined,
      phoneNumber: this.phoneNumber() || undefined,
      email: this.email() || undefined,
      paymentDetails: this.paymentDetails() || undefined,
      logoUrl: this.logoUrl() || undefined,
    };
    this.companyService.createCompany(command).subscribe({
      next: (response) => {
        this.companyStore.selectCompany({
          id: response.id,
          name: this.name(),
          address: this.address(),
          taxNumber: this.taxNumber(),
          phoneNumber: this.phoneNumber(),
          email: this.email(),
          paymentDetails: this.paymentDetails(),
          logoUrl: this.logoUrl(),
        });
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
