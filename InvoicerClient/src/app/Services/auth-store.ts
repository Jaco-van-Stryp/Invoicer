import { computed, Injectable, signal } from '@angular/core';

const TOKEN_KEY = 'auth_token';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private tokenSignal = signal<string | null>(this.loadToken());

  readonly token = this.tokenSignal.asReadonly();
  readonly isLoggedIn = computed(() => !!this.tokenSignal());

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this.tokenSignal.set(token);
  }

  clearToken(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.tokenSignal.set(null);
  }

  private loadToken(): string | null {
    if (typeof localStorage === 'undefined') {
      return null;
    }
    return localStorage.getItem(TOKEN_KEY);
  }
}
