import { computed, Injectable, signal } from '@angular/core';

const TOKEN_KEY = 'auth_token';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private tokenSignal = signal<string | null>(this.loadToken());

  readonly token = this.tokenSignal.asReadonly();
  readonly isLoggedIn = computed(() => !!this.tokenSignal());

  readonly userEmail = computed<string | null>(() => {
    const token = this.tokenSignal();
    if (!token) return null;
    try {
      const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
      const payload = JSON.parse(atob(padded));
      return (
        (payload[
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
        ] as string) ?? null
      );
    } catch {
      return null;
    }
  });

  setToken(token: string): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(TOKEN_KEY, token);
    }
    this.tokenSignal.set(token);
  }

  clearToken(): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(TOKEN_KEY);
    }
    this.tokenSignal.set(null);
  }

  private loadToken(): string | null {
    if (typeof localStorage === 'undefined') {
      return null;
    }
    return localStorage.getItem(TOKEN_KEY);
  }
}
