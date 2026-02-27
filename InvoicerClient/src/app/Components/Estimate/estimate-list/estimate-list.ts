import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { EstimateService, GetAllEstimatesResponse, EstimateStatus } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { EstimateFormDialog } from '../estimate-form-dialog/estimate-form-dialog';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-estimate-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    SelectModule,
    TagModule,
    TooltipModule,
    EstimateFormDialog,
  ],
  host: { class: 'block' },
  styleUrl: './estimate-list.css',
  templateUrl: './estimate-list.html',
})
export class EstimateList implements OnInit {
  estimateService = inject(EstimateService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);
  confirmationService = inject(ConfirmationService);

  estimates = signal<GetAllEstimatesResponse[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  selectedEstimate = signal<GetAllEstimatesResponse | null>(null);

  // Filter state
  searchText = signal('');
  selectedStatuses = signal<EstimateStatus[]>([]);
  filterMonth = signal<number | null>(null);
  filterYear = signal<number | null>(null);

  readonly statusOptions: { label: string; value: EstimateStatus }[] = [
    { label: 'Draft', value: EstimateStatus.Draft },
    { label: 'Sent', value: EstimateStatus.Sent },
    { label: 'Accepted', value: EstimateStatus.Accepted },
    { label: 'Declined', value: EstimateStatus.Declined },
  ];

  availableYears = computed(() => {
    const years = new Set<number>();
    for (const est of this.estimates()) {
      if (est.estimateDate) years.add(new Date(est.estimateDate).getFullYear());
    }
    return [...years].sort((a, b) => b - a).map(y => ({ label: String(y), value: y }));
  });

  availableMonths = computed(() => {
    const year = this.filterYear();
    const months = new Set<number>();
    for (const est of this.estimates()) {
      if (!est.estimateDate) continue;
      const d = new Date(est.estimateDate);
      if (year !== null && d.getFullYear() !== year) continue;
      months.add(d.getMonth());
    }
    return [...months].sort((a, b) => a - b).map(m => ({ label: MONTH_NAMES[m], value: m }));
  });

  filteredEstimates = computed(() => {
    const search = this.searchText().toLowerCase().trim();
    const statuses = this.selectedStatuses();
    const month = this.filterMonth();
    const year = this.filterYear();

    return this.estimates().filter(est => {
      if (
        search &&
        !est.estimateNumber?.toLowerCase().includes(search) &&
        !est.clientName?.toLowerCase().includes(search)
      ) {
        return false;
      }
      if (statuses.length > 0 && (!est.status || !statuses.includes(est.status))) {
        return false;
      }
      if (month !== null || year !== null) {
        const d = est.estimateDate ? new Date(est.estimateDate) : null;
        if (!d) return false;
        if (month !== null && d.getMonth() !== month) return false;
        if (year !== null && d.getFullYear() !== year) return false;
      }
      return true;
    });
  });

  hasActiveFilters = computed(
    () =>
      this.searchText().trim().length > 0 ||
      this.selectedStatuses().length > 0 ||
      this.filterMonth() !== null ||
      this.filterYear() !== null,
  );

  ngOnInit() {
    this.loadEstimates();
  }

  loadEstimates() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loading.set(true);
    this.estimateService.getAllEstimates(companyId).subscribe({
      next: (r) => this.estimates.set(r),
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load estimates',
        });
      },
      complete: () => this.loading.set(false),
    });
  }

  toggleStatus(status: EstimateStatus) {
    const current = this.selectedStatuses();
    if (current.includes(status)) {
      this.selectedStatuses.set(current.filter(s => s !== status));
    } else {
      this.selectedStatuses.set([...current, status]);
    }
  }

  onYearChange(year: number | null) {
    this.filterYear.set(year);
    this.filterMonth.set(null);
  }

  clearFilters() {
    this.searchText.set('');
    this.selectedStatuses.set([]);
    this.filterMonth.set(null);
    this.filterYear.set(null);
  }

  openNew() {
    this.selectedEstimate.set(null);
    this.dialogVisible.set(true);
  }

  editEstimate(estimate: GetAllEstimatesResponse) {
    this.selectedEstimate.set(estimate);
    this.dialogVisible.set(true);
  }

  deleteEstimate(estimate: GetAllEstimatesResponse) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete estimate ${estimate.estimateNumber}?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { severity: 'danger', label: 'Yes, Delete' },
      rejectButtonProps: { severity: 'secondary', outlined: true, label: 'Cancel' },
      accept: () => {
        if (!estimate.id) return;
        this.estimateService.deleteEstimate(estimate.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Estimate deleted',
            });
            this.loadEstimates();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete estimate',
            });
          },
        });
      },
    });
  }

  onSaved() {
    this.loadEstimates();
  }

  statusSeverity(
    status: EstimateStatus | undefined,
  ): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
    switch (status) {
      case EstimateStatus.Accepted:
        return 'success';
      case EstimateStatus.Sent:
        return 'info';
      case EstimateStatus.Draft:
        return 'secondary';
      case EstimateStatus.Declined:
        return 'danger';
      default:
        return undefined;
    }
  }
}
