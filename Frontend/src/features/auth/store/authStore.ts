import { create } from "zustand";

export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  provider: string;
};

export type AuthSession = {
  token: string;
  expiresAt: string;
  user: AuthUser;
};

type AuthStore = {
  session: AuthSession | null;
  isAuthenticated: boolean;
  login: (session: AuthSession) => void;
  logout: () => void;
};

const STORAGE_KEY = "auth.session";

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

export const useAuthStore = create<AuthStore>((set) => ({
  session: loadSession(),
  isAuthenticated: loadSession() !== null,
  login: (session) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    set({ session, isAuthenticated: true });
  },
  logout: () => {
    localStorage.removeItem(STORAGE_KEY);
    set({ session: null, isAuthenticated: false });
  },
}));

export function getStoredToken(): string | null {
  return useAuthStore.getState().session?.token ?? null;
}
