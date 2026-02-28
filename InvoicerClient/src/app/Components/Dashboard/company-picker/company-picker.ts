import { Component, inject, OnInit, output, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CompanyService, GetAllCompaniesResponse } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { Logo } from '../../General/logo/logo';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-company-picker',
  imports: [RouterLink, ButtonModule, Logo],
  host: { class: 'block' },
  styleUrl: './company-picker.css',
  templateUrl: './company-picker.html',
})
export class CompanyPicker implements OnInit {
  companyService = inject(CompanyService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  saved = output<GetAllCompaniesResponse>();

  companies = signal<GetAllCompaniesResponse[]>([]);
  loading = signal(true);

  showCompanyList = computed(() => !this.loading() && this.companies().length > 0);
  showEmptyState = computed(() => !this.loading() && this.companies().length === 0);

  ngOnInit() {
    this.loadCompanies();
  }

  loadCompanies() {
    this.loading.set(true);
    this.companyService.getAllCompanies().subscribe({
      next: (response) => {
        this.companies.set(response);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load companies.',
        });
        this.loading.set(false);
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  selectCompany(company: GetAllCompaniesResponse) {
    this.companyStore.selectCompany(company);
    this.saved.emit(company);
    this.messageService.add({
      severity: 'success',
      summary: 'Company Selected',
      detail: `Now working with ${company.name ?? 'Unknown Company'}`,
    });
  }
}
