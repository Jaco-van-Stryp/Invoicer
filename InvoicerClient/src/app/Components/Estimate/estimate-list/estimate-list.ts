import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { Table, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { EstimateService, GetAllEstimatesResponse, EstimateStatus } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { EstimateFormDialog } from '../estimate-form-dialog/estimate-form-dialog';

@Component({
  selector: 'app-estimate-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    TableModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    ConfirmDialogModule,
    TagModule,
    TooltipModule,
    EstimateFormDialog,
  ],
  providers: [MessageService, ConfirmationService],
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
  viewMode = signal<'table' | 'cards'>('cards');

  dt = viewChild<Table>('dt');

  ngOnInit() {
    this.loadEstimates();
  }

  toggleView() {
    this.viewMode.update((mode) => (mode === 'table' ? 'cards' : 'table'));
  }

  loadEstimates() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loading.set(true);
    this.estimateService.getAllEstimates(companyId).subscribe({
      next: (r) => this.estimates.set(r),
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false),
    });
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

  onFilter(event: Event) {
    const table = this.dt();
    if (table) {
      table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }
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
