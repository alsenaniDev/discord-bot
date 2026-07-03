import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import {
  ActivityTimelineGroupId,
  ActivityTimelineModel,
  ActivityTimelineRow
} from '../../../../core/models/mission-control.models';
import { resolveModuleNameKey } from '../../../../core/services/activity-timeline-legacy.parser';

@Component({
  selector: 'app-activity-timeline',
  templateUrl: './activity-timeline.component.html',
  styleUrls: ['./activity-timeline.component.css']
})
export class ActivityTimelineComponent {
  @Input() model: ActivityTimelineModel | null = null;
  @Output() rowClick = new EventEmitter<string>();
  @Output() viewAllClick = new EventEmitter<void>();
  @Output() retryClick = new EventEmitter<void>();

  constructor(private translate: TranslateService) {}

  get hasRows(): boolean {
    return !!this.model?.groups.some(group => group.rows.length > 0);
  }

  rowMessage(row: ActivityTimelineRow): string {
    const params: Record<string, string | number> = { ...(row.messageParams ?? {}) };

    if (typeof params['moduleNameKey'] === 'string') {
      const localized = this.translate.instant(
        `overview.v2.activity.moduleNames.${params['moduleNameKey']}`
      );
      const fallbackKey = `overview.v2.activity.moduleNames.${params['moduleNameKey']}`;
      if (localized !== fallbackKey) {
        params['moduleName'] = localized;
      }
      delete params['moduleNameKey'];
    } else if (typeof params['moduleName'] === 'string') {
      const moduleNameKey = resolveModuleNameKey(String(params['moduleName']));
      if (moduleNameKey) {
        params['moduleName'] = this.translate.instant(
          `overview.v2.activity.moduleNames.${moduleNameKey}`
        );
      }
    }

    return this.translate.instant(row.messageKey, params);
  }

  relativeTime(isoDate: string, groupId: ActivityTimelineGroupId): string {
    const date = new Date(isoDate);
    if (Number.isNaN(date.getTime())) {
      return this.translate.instant('common.emptyValue');
    }

    const now = new Date();
    const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const startOfYesterday = new Date(startOfToday);
    startOfYesterday.setDate(startOfYesterday.getDate() - 1);

    if (groupId === 'yesterday' || (date >= startOfYesterday && date < startOfToday)) {
      return this.translate.instant('overview.v2.activity.time.yesterday');
    }

    if (groupId === 'earlier' || date < startOfYesterday) {
      const diffDays = Math.max(1, Math.floor((startOfToday.getTime() - date.getTime()) / (24 * 60 * 60 * 1000)));
      return this.formatDays(diffDays);
    }

    const diffMs = Date.now() - date.getTime();
    const diffMinutes = Math.max(0, Math.floor(diffMs / (60 * 1000)));

    if (diffMinutes < 1) {
      return this.translate.instant('overview.v2.activity.time.justNow');
    }

    if (diffMinutes < 60) {
      return this.formatMinutes(diffMinutes);
    }

    const diffHours = Math.floor(diffMinutes / 60);
    return this.formatHours(diffHours);
  }

  rowAriaLabel(row: ActivityTimelineRow, groupId: ActivityTimelineGroupId): string {
    const time = this.relativeTime(row.occurredAt, groupId);
    const message = this.rowMessage(row);

    if (row.route) {
      return this.translate.instant('overview.v2.activity.rowAriaLink', { message, time });
    }

    return this.translate.instant('overview.v2.activity.rowAriaStatic', { message, time });
  }

  onRowClick(row: ActivityTimelineRow, event: Event): void {
    if (!row.route) {
      event.preventDefault();
      return;
    }

    this.rowClick.emit(row.route);
  }

  private formatMinutes(count: number): string {
    if (count === 1) {
      return this.translate.instant('overview.v2.activity.time.minuteAgo');
    }

    if (this.isArabic() && count === 2) {
      return this.translate.instant('overview.v2.activity.time.minutesAgoDual');
    }

    return this.translate.instant('overview.v2.activity.time.minutesAgo', { count });
  }

  private formatHours(count: number): string {
    if (count === 1) {
      return this.translate.instant('overview.v2.activity.time.hourAgo');
    }

    return this.translate.instant('overview.v2.activity.time.hoursAgo', { count });
  }

  private formatDays(count: number): string {
    if (count === 1) {
      return this.translate.instant('overview.v2.activity.time.dayAgo');
    }

    return this.translate.instant('overview.v2.activity.time.daysAgo', { count });
  }

  private isArabic(): boolean {
    return (this.translate.currentLang || this.translate.defaultLang || '').startsWith('ar');
  }
}
