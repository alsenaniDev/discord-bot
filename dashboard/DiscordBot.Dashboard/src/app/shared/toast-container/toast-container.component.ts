import { Component } from '@angular/core';
import { ToastMessage, ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  templateUrl: './toast-container.component.html',
  styleUrls: ['./toast-container.component.css']
})
export class ToastContainerComponent {
  toasts: ToastMessage[] = [];

  constructor(private toast: ToastService) {
    this.toast.toasts$.subscribe(toasts => {
      this.toasts = toasts;
    });
  }

  dismiss(id: number): void {
    this.toast.dismiss(id);
  }
}
