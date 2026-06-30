import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error';

export interface ToastMessage {
  id: number;
  message: string;
  type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
  readonly toasts$ = this.toastsSubject.asObservable();

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string): void {
    this.show(message, 'error');
  }

  dismiss(id: number): void {
    this.toastsSubject.next(this.toastsSubject.value.filter(t => t.id !== id));
  }

  private show(message: string, type: ToastType): void {
    const toast: ToastMessage = { id: this.nextId++, message, type };
    const toasts = [...this.toastsSubject.value, toast];
    this.toastsSubject.next(toasts);

    setTimeout(() => this.dismiss(toast.id), 4500);
  }
}
