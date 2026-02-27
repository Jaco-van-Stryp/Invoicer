import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { ClientService, GetAllClientsResponse } from '../../../api';
import { CompanyStore } from '../../../Services/company-store';
import { ClientFormDialog } from '../client-form-dialog/client-form-dialog';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-client-list',
  imports: [
    FormsModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    ConfirmDialogModule,
    TooltipModule,
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
  searchQuery = signal('');

  filteredClients = computed(() => {
    const q = this.searchQuery().toLowerCase();
    if (!q) return this.clients();
    return this.clients().filter(
      (c) =>
        c.name?.toLowerCase().includes(q) ||
        c.email?.toLowerCase().includes(q) ||
        c.phoneNumber?.toLowerCase().includes(q),
    );
  });

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
      acceptButtonProps: { severity: 'danger', label: 'Yes, Delete' },
      rejectButtonProps: { severity: 'secondary', outlined: true, label: 'Cancel' },
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

  getInitials(name: string | null | undefined): string {
    if (!name) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  onSaved() {
    this.loadClients();
  }
}
