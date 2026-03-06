import { ChangeDetectionStrategy, Component, PLATFORM_ID, afterNextRender, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthStore } from '../../../Services/auth-store';
import { Copyright } from '../../General/copyright/copyright';
import { Logo } from '../../General/logo/logo';
import { LottieAnimation } from '../../General/lottie-animation/lottie-animation';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-landing',
  imports: [
    ButtonModule,
    RouterLink,
    Copyright,
    Logo,
    LottieAnimation,
  ],
  host: { class: 'block' },
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing {
  private router = inject(Router);
  private authStore = inject(AuthStore);
  private platformId = inject(PLATFORM_ID);

  isVisible = signal(false);

  features = [
    {
      icon: 'pi pi-file-edit',
      title: 'Professional Invoices',
      description:
        'Create polished, branded invoices in seconds. Add your logo, customize details, and send them directly to clients — no design skills needed.',
    },
    {
      icon: 'pi pi-file-check',
      title: 'Estimates & Quotes',
      description:
        'Win more clients with professional estimates. Track quote status and convert accepted estimates to invoices in one click.',
    },
    {
      icon: 'pi pi-users',
      title: 'Client Management',
      description:
        'Keep all your clients organized in one place. Track contact details, invoice history, and outstanding balances at a glance.',
    },
    {
      icon: 'pi pi-box',
      title: 'Product Catalog',
      description:
        'Build a reusable product and service catalog. Add items to invoices instantly — no retyping, no mistakes, every time.',
    },
    {
      icon: 'pi pi-building',
      title: 'Multi-Company Support',
      description:
        'Run multiple businesses from a single account. Switch between companies effortlessly with fully isolated data.',
    },
    {
      icon: 'pi pi-credit-card',
      title: 'Payment Tracking',
      description:
        'Record and track payments against every invoice. Always know what\'s paid, what\'s pending, and what\'s overdue.',
    },
  ];

  stats = [
    { value: '10x', label: 'Faster Invoicing' },
    { value: '100%', label: 'Free Forever' },
    { value: '60s', label: 'To First Invoice' },
    { value: '∞', label: 'Invoices Per Month' },
  ];

  constructor() {
    afterNextRender(() => {
      this.isVisible.set(true);
      if (this.authStore.isLoggedIn()) {
        this.router.navigate(['/dashboard']);
      }
    });
  }

  scrollTo(sectionId: string) {
    if (isPlatformBrowser(this.platformId)) {
      document.getElementById(sectionId)?.scrollIntoView({ behavior: 'smooth' });
    }
  }
}
