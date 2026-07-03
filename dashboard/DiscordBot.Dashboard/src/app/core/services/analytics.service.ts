import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  track(event: string, properties?: Record<string, unknown>): void {
    const payload = {
      event,
      timestamp: new Date().toISOString(),
      ...properties
    };

    if (!environment.production) {
      console.info('[analytics]', payload);
    }
  }
}
