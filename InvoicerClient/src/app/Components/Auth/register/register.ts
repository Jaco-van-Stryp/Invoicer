import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { AbstractControl, FormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputOtpModule } from 'primeng/inputotp';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { AuthService, LoginResponse, RegisterCommand } from '../../../api';
import { AuthStore } from '../../../Services/auth-store';
import { Copyright } from '../../General/copyright/copyright';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-register',
  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    CardModule,
    FloatLabelModule,
    InputOtpModule,
    InputTextModule,
    Copyright,
    TooltipModule,
  ],
  host: { class: 'block' },
  styleUrl: './register.css',
  templateUrl: './register.html',
})
export class Register implements OnDestroy {
  router = inject(Router);
  authService = inject(AuthService);
  authStore = inject(AuthStore);
  messageService = inject(MessageService);

  step = signal<'email' | 'otp'>('email');
  loading = signal(false);
  email = signal('');
  otpCode = signal('');
  accessTokenKey = signal('');

  resendTimer = signal(0);
  private resendInterval: any;

  isEmailValid = computed(() => {
    const email = this.email();
    if (!email) return false;
    return Validators.email({ value: email } as AbstractControl) === null;
  });
  isOtpComplete = computed(() => this.otpCode().length === 6);

  register() {
    this.loading.set(true);
    const command: RegisterCommand = { email: this.email() };
    this.authService.register(command).subscribe({
      next: (response) => {
        this.accessTokenKey.set(response.accessTokenKey ?? '');
        this.otpCode.set('');
        this.step.set('otp');
        this.startResendTimer();
        this.messageService.add({
          severity: 'success',
          summary: 'Code Sent',
          detail: 'Check your email for the verification code',
        });
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to create account. Please try again.',
        });
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  verify() {
    this.loading.set(true);
    this.authService
      .login({
        email: this.email(),
        accessTokenKey: this.accessTokenKey(),
        accessToken: this.otpCode(),
      })
      .subscribe({
        next: (response: LoginResponse) => {
          if (response.token) {
            this.authStore.setToken(response.token);
            this.messageService.add({
              severity: 'success',
              summary: 'Account Created',
              detail: 'Welcome to Invoicer!',
            });
            this.router.navigate(['/create-company']);
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Verification Failed',
              detail: 'No token received. Please try again.',
            });
          }
        },
        error: () => {
          this.loading.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Invalid verification code. Please try again.',
          });
        },
        complete: () => {
          this.loading.set(false);
        },
      });
  }

  resendCode() {
    this.loading.set(true);
    this.authService.register({ email: this.email() }).subscribe({
      next: (response) => {
        this.accessTokenKey.set(response.accessTokenKey ?? '');
        this.otpCode.set('');
        this.startResendTimer();
        this.messageService.add({
          severity: 'success',
          summary: 'Code Resent',
          detail: 'A new verification code has been sent',
        });
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to resend code. Please try again.',
        });
      },
      complete: () => {
        this.loading.set(false);
      },
    });
  }

  startResendTimer() {
    if (this.resendInterval) {
      clearInterval(this.resendInterval);
      this.resendInterval = null;
    }
    this.resendTimer.set(120);
    this.resendInterval = setInterval(() => {
      this.resendTimer.update((value) => value - 1);
      if (this.resendTimer() === 0) {
        clearInterval(this.resendInterval);
        this.resendInterval = null;
      }
    }, 1000);
  }

  backToEmail() {
    this.step.set('email');
    clearInterval(this.resendInterval);
    this.resendTimer.set(0);
  }

  ngOnDestroy() {
    clearInterval(this.resendInterval);
  }
}
