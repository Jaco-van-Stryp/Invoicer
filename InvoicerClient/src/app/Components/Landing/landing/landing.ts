import { Component, inject, signal, computed, afterNextRender, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { RippleModule } from 'primeng/ripple';
import { APIService, JoinWaitingListCommand } from '../../../api';
import { Copyright } from '../../General/copyright/copyright';
import { Logo } from '../../General/logo/logo';
import { LottieAnimation } from '../../General/lottie-animation/lottie-animation';

@Component({
  selector: 'app-landing',
  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    InputTextModule,
    RippleModule,
    Copyright,
    Logo,
    LottieAnimation,
  ],
  host: { class: 'block' },
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing {
  private apiService = inject(APIService);
  private messageService = inject(MessageService);
  private platformId = inject(PLATFORM_ID);

  email = signal('');
  loading = signal(false);
  submitted = signal(false);
  activeFeature = signal(0);
  isVisible = signal(false);

  isEmailValid = computed(() => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(this.email());
  });

  features = [
    {
      icon: 'pi pi-file-edit',
      title: 'Professional Invoices',
      description:
        'Create polished, branded invoices in seconds. Customize templates, add your logo, and impress every client.',
    },
    {
      icon: 'pi pi-users',
      title: 'Client Management',
      description:
        'Keep all your clients organized in one place. Track contact details, invoice history, and outstanding balances.',
    },
    {
      icon: 'pi pi-box',
      title: 'Product Catalog',
      description:
        'Maintain a reusable product & service catalog. Add items to invoices with one click — no retyping.',
    },
    {
      icon: 'pi pi-building',
      title: 'Multi-Company',
      description:
        'Run multiple businesses from a single account. Switch between companies effortlessly with isolated data.',
    },
    {
      icon: 'pi pi-lock',
      title: 'Passwordless Auth',
      description:
        'No passwords to remember or reset. Sign in securely with a one-time code sent straight to your inbox.',
    },
    {
      icon: 'pi pi-cloud-upload',
      title: 'Cloud Storage',
      description:
        'Upload receipts, contracts, and attachments. Everything synced and accessible from anywhere, anytime.',
    },
  ];

  stats = [
    { value: '10x', label: 'Faster Invoicing' },
    { value: '100%', label: 'Free During Beta' },
    { value: '0', label: 'Passwords Needed' },
    { value: '∞', label: 'Invoices Per Month' },
  ];

  constructor() {
    afterNextRender(() => {
      this.isVisible.set(true);
    });
  }

  joinWaitingList() {
    if (!this.isEmailValid() || this.loading()) return;

    this.loading.set(true);
    const command: JoinWaitingListCommand = { email: this.email() };

    this.apiService.apiJoinPost(command).subscribe({
      next: () => {
        this.submitted.set(true);
        this.loading.set(false);
        this.messageService.add({
          severity: 'success',
          summary: "You're on the list!",
          detail: "We'll notify you as soon as Invoicer is ready.",
          life: 5000,
        });
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Something went wrong',
          detail: 'Please try again in a moment.',
          life: 5000,
        });
      },
    });
  }

  scrollTo(sectionId: string) {
    if (isPlatformBrowser(this.platformId)) {
      document.getElementById(sectionId)?.scrollIntoView({ behavior: 'smooth' });
    }
  }
}
