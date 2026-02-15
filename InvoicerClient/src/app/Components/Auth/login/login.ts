import { Component, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { FloatLabelModule } from 'primeng/floatlabel';
import { InputOtpModule } from 'primeng/inputotp';
import { InputTextModule } from 'primeng/inputtext';
import { AuthService, GetAccessTokenQuery, LoginResponse } from '../../../api';
import { AuthStore } from '../../../Services/auth-store';
import { Copyright } from '../../General/copyright/copyright';
import { Logo } from '../../General/logo/logo';
import { LottieAnimation } from "../../General/lottie-animation/lottie-animation";

@Component({
  selector: 'app-login',
  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    CardModule,
    DividerModule,
    FloatLabelModule,
    InputOtpModule,
    InputTextModule,
    Copyright,
    Logo,
    LottieAnimation
],
  host: { class: 'block' },
  templateUrl: './login.html',
})
export class Login {
  router = inject(Router);
  authService = inject(AuthService);
  authStore = inject(AuthStore);
  messageService = inject(MessageService);

  step = signal<'email' | 'otp'>('email');
  loading = signal(false);
  otpLoading = signal(false);
  email = signal('');
  otpCode = signal('');
  accessTokenKey = signal('');

  isEmailValid = computed(() => {
    const email = this.email();
    if (!email) return false;
    return Validators.email({ value: email } as AbstractControl) === null;
  });
  isOtpComplete = computed(() => this.otpCode().length === 6);

  requestOtp() {
    this.loading.set(true);
    this.otpLoading.set(true);
    const query: GetAccessTokenQuery = { email: this.email() };
    this.authService.getAccessToken(query).subscribe({
      next: (response) => {
        this.accessTokenKey.set(response.accessTokenKey ?? '');
        this.otpCode.set('');
        this.step.set('otp');
        this.messageService.add({
          severity: 'success',
          summary: 'Code Sent',
          detail: 'Check your email for the verification code',
        });
      },
      error: () => {
        this.loading.set(false);
        this.otpLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to send verification code. Please try again.',
        });
      },
      complete: () => {
        this.loading.set(false);
        this.otpLoading.set(false);
      },
    });
  }

  login() {
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
              summary: 'Success',
              detail: 'Logged in successfully',
            });
            this.router.navigate(['/dashboard']);
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Authentication Failed',
              detail: 'No token received. Please try again.',
            });
          }
        },
        error: () => {
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
}
