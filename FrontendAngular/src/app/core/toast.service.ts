import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  readonly id: string;
  readonly kind: 'success' | 'error' | 'info';
  readonly message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<ToastMessage[]>([]);

  show(kind: ToastMessage['kind'], message: string, durationMs = 4000): void {
    const id = crypto.randomUUID();
    this.toasts.update((ts) => [...ts, { id, kind, message }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }

  dismiss(id: string): void {
    this.toasts.update((ts) => ts.filter((t) => t.id !== id));
  }
}
