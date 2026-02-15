import { computed, Injectable, signal } from '@angular/core';
import { GetAllCompaniesResponse } from '../api';

const COMPANY_KEY = 'selected_company';

@Injectable({ providedIn: 'root' })
export class CompanyStore {
  private companySignal = signal<GetAllCompaniesResponse | null>(this.loadCompany());

  readonly company = this.companySignal.asReadonly();
  readonly hasCompany = computed(() => !!this.companySignal());

  selectCompany(company: GetAllCompaniesResponse): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(COMPANY_KEY, JSON.stringify(company));
    }
    this.companySignal.set(company);
  }

  clearCompany(): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(COMPANY_KEY);
    }
    this.companySignal.set(null);
  }

  private loadCompany(): GetAllCompaniesResponse | null {
    if (typeof localStorage === 'undefined') {
      return null;
    }
    const stored = localStorage.getItem(COMPANY_KEY);
    if (!stored) return null;
    try {
      return JSON.parse(stored) as GetAllCompaniesResponse;
    } catch {
      return null;
    }
  }
}
