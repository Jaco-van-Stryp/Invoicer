import { Component, computed, effect, inject, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { FileUploadModule } from 'primeng/fileupload';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CompanyService, FileService } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-edit-company-dialog',
  imports: [
    FormsModule,
    DialogModule,
    ButtonModule,
    FileUploadModule,
    FloatLabelModule,
    InputTextModule,
    TextareaModule,
  ],
  host: { class: 'block' },
  styleUrl: './edit-company-dialog.css',
  templateUrl: './edit-company-dialog.html',
})
export class EditCompanyDialog {
  companyService = inject(CompanyService);
  fileService = inject(FileService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  visible = model(false);
  saved = output<void>();

  name = signal('');
  email = signal('');
  phoneNumber = signal('');
  taxNumber = signal('');
  address = signal('');
  paymentDetails = signal('');
  logoUrl = signal('');
  logoPreview = signal<string | null>(null);
  uploadingLogo = signal(false);
  saving = signal(false);

  isFormValid = computed(() => this.name().trim().length > 0 && !this.uploadingLogo());

  constructor() {
    effect(() => {
      if (this.visible()) {
        const company = this.companyStore.company();
        if (company) {
          this.name.set(company.name ?? '');
          this.email.set(company.email ?? '');
          this.phoneNumber.set(company.phoneNumber ?? '');
          this.taxNumber.set(company.taxNumber ?? '');
          this.address.set(company.address ?? '');
          this.paymentDetails.set(company.paymentDetails ?? '');
          this.logoUrl.set(company.logoUrl ?? '');
          this.logoPreview.set(company.logoUrl ?? null);
        }
      }
    });
  }

  onLogoSelected(event: { files: File[] }) {
    const file = event.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => this.logoPreview.set(reader.result as string);
    reader.readAsDataURL(file);

    this.uploadingLogo.set(true);
    this.fileService.uploadFile(file).subscribe({
      next: (url) => {
        this.logoUrl.set(url);
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
        this.logoPreview.set(this.companyStore.company()?.logoUrl ?? null);
      },
      complete: () => this.uploadingLogo.set(false),
    });
  }

  removeLogo() {
    this.logoUrl.set('');
    this.logoPreview.set(null);
  }

  save() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.saving.set(true);
    this.companyService
      .updateCompanyDetails({
        companyId,
        name: this.name(),
        email: this.email() || null,
        phoneNumber: this.phoneNumber() || null,
        taxNumber: this.taxNumber() || null,
        address: this.address() || null,
        paymentDetails: this.paymentDetails() || null,
        logoUrl: this.logoUrl() || null,
      })
      .subscribe({
        next: () => {
          this.companyStore.selectCompany({
            ...this.companyStore.company()!,
            name: this.name(),
            email: this.email() || undefined,
            phoneNumber: this.phoneNumber() || undefined,
            taxNumber: this.taxNumber() || undefined,
            address: this.address() || undefined,
            paymentDetails: this.paymentDetails() || undefined,
            logoUrl: this.logoUrl() || undefined,
          });
          this.messageService.add({
            severity: 'success',
            summary: 'Saved',
            detail: 'Company details updated successfully.',
          });
          this.visible.set(false);
          this.saved.emit();
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update company details.',
          });
        },
        complete: () => this.saving.set(false),
      });
  }
}
