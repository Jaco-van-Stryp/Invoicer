import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { Table, TableModule } from 'primeng/table';
import { ClientService, GetAllClientsResponse } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { ClientFormDialog } from '../client-form-dialog/client-form-dialog';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-client-list',
  imports: [
    TableModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    ConfirmDialogModule,
    ClientFormDialog,
  ],
  host: { class: 'block' },
  styleUrl: './client-list.css',
  templateUrl: './client-list.html',
})
export class ClientList implements OnInit {
  clientService = inject(ClientService);
  companyStore = inject(CompanyStore);
  messageService = inject(MessageService);
  confirmationService = inject(ConfirmationService);

  clients = signal<GetAllClientsResponse[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  selectedClient = signal<GetAllClientsResponse | null>(null);

  dt = viewChild<Table>('dt');

  ngOnInit() {
    this.loadClients();
  }

  loadClients() {
    const companyId = this.companyStore.company()?.id;
    if (!companyId) return;

    this.loading.set(true);
    this.clientService.getAllClients(companyId).subscribe({
      next: (r) => this.clients.set(r),
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load clients.',
        });
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  openNew() {
    this.selectedClient.set(null);
    this.dialogVisible.set(true);
  }

  editClient(client: GetAllClientsResponse) {
    this.selectedClient.set(client);
    this.dialogVisible.set(true);
  }

  deleteClient(client: GetAllClientsResponse) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${client.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        const companyId = this.companyStore.company()?.id;
        if (!companyId || !client.id) return;

        this.clientService.deleteClient(companyId, client.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Deleted',
              detail: 'Client deleted successfully.',
            });
            this.loadClients();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete client.',
            });
          },
        });
      },
    });
  }

  onFilter(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.dt()?.filterGlobal(value, 'contains');
  }

  onSaved() {
    this.loadClients();
  }
}
