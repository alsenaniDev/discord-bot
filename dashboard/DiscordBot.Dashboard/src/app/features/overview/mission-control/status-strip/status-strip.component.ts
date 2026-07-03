import { Component, Input } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { StatusStripModel } from '../../../../core/models/mission-control.models';

@Component({
  selector: 'app-status-strip',
  templateUrl: './status-strip.component.html',
  styleUrls: ['./status-strip.component.css']
})
export class StatusStripComponent {
  @Input() loading = false;
  @Input() model: StatusStripModel | null = null;

  constructor(private translate: TranslateService) {}

  get syncLabel(): string {
    if (this.loading) {
      return '…';
    }

    if (!this.model) {
      return '';
    }

    if (this.model.syncing) {
      return this.translate.instant('overview.v2.status.syncing');
    }

    if (!this.model.resourcesSyncedAt) {
      return this.translate.instant('overview.v2.status.notSynced');
    }

    const syncedAt = new Date(this.model.resourcesSyncedAt);
    if (Number.isNaN(syncedAt.getTime())) {
      return this.translate.instant('overview.v2.status.notSynced');
    }

    const relative = this.formatRelativeTime(syncedAt);
    return this.translate.instant('overview.v2.status.syncedAgo', { time: relative });
  }

  get syncDateTime(): string | null {
    if (!this.model?.resourcesSyncedAt) {
      return null;
    }

    const syncedAt = new Date(this.model.resourcesSyncedAt);
    if (Number.isNaN(syncedAt.getTime())) {
      return null;
    }

    return syncedAt.toISOString();
  }

  private formatRelativeTime(date: Date): string {
    const diffMs = Date.now() - date.getTime();
    const diffMinutes = Math.floor(diffMs / (60 * 1000));

    if (diffMinutes < 1) {
      return this.translate.instant('overview.v2.status.justNow');
    }

    if (diffMinutes < 60) {
      return this.translate.instant('overview.v2.status.minutesAgo', { count: diffMinutes });
    }

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) {
      return this.translate.instant('overview.v2.status.hoursAgo', { count: diffHours });
    }

    const diffDays = Math.floor(diffHours / 24);
    return this.translate.instant('overview.v2.status.daysAgo', { count: diffDays });
  }
}
