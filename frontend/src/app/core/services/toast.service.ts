import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: string;
  message: string;
  type: ToastType;
  durationMs: number;
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  show(message: string, type: ToastType = 'info', durationMs: number = 4000): string {
    const id = `toast-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
    const newToast: Toast = { id, message, type, durationMs };

    this._toasts.update((current) => [...current, newToast]);

    if (durationMs > 0) {
      setTimeout(() => {
        this.remove(id);
      }, durationMs);
    }

    return id;
  }

  success(message: string, durationMs: number = 4000): string {
    return this.show(message, 'success', durationMs);
  }

  error(message: string, durationMs: number = 5000): string {
    return this.show(message, 'error', durationMs);
  }

  info(message: string, durationMs: number = 4000): string {
    return this.show(message, 'info', durationMs);
  }

  warning(message: string, durationMs: number = 4500): string {
    return this.show(message, 'warning', durationMs);
  }

  remove(id: string): void {
    this._toasts.update((current) => current.filter((t) => t.id !== id));
  }

  clear(): void {
    this._toasts.set([]);
  }
}
