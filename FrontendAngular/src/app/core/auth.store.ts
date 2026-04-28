import { computed, signal } from '@angular/core';

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  provider: string;
}

export interface AuthSession {
  token: string;
  expiresAt: string;
  user: AuthUser;
}

const STORAGE_KEY = 'auth.session';

function loadSession(): AuthSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as AuthSession;
    if (new Date(parsed.expiresAt) <= new Date()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

const _session = signal<AuthSession | null>(loadSession());

export const session = _session.asReadonly();
export const isAuthenticated = computed(() => _session() !== null);

export function login(s: AuthSession): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
  _session.set(s);
}

export function logout(): void {
  localStorage.removeItem(STORAGE_KEY);
  _session.set(null);
}

export function getToken(): string | null {
  return _session()?.token ?? null;
}
