import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { CompanyService, GetAllCompaniesResponse } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { Copyright } from '../../General/copyright/copyright';
import { Logo } from '../../General/logo/logo';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, ButtonModule, CardModule, Copyright, Logo],
  host: { class: 'block' },
  styleUrl: './dashboard.css',
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  router = inject(Router);
  companyService = inject(CompanyService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);

  companies = signal<GetAllCompaniesResponse[]>([]);
  loading = signal(true);

  selectedCompany = this.companyStore.company;
  hasSelectedCompany = this.companyStore.hasCompany;

  showCompanyList = computed(() => !this.hasSelectedCompany() && this.companies().length > 0);
  showEmptyState = computed(() => !this.loading() && this.companies().length === 0);

  ngOnInit() {
    this.loadCompanies();
  }

  loadCompanies() {
    this.loading.set(true);
    this.companyService.getAllCompanies().subscribe({
      next: (response) => {
        this.companies.set(response);
        if (this.hasSelectedCompany()) {
          const still = response.find((c) => c.id === this.selectedCompany()?.id);
          if (!still) {
            this.companyStore.clearCompany();
          }
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load companies.',
        });
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  selectCompany(company: GetAllCompaniesResponse) {
    this.companyStore.selectCompany(company);
    this.messageService.add({
      severity: 'success',
      summary: 'Company Selected',
      detail: `Now working with ${company.name}`,
    });
  }

  switchCompany() {
    this.companyStore.clearCompany();
  }
}
