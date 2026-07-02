import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildAccessService } from '../../core/services/guild-access.service';
import { ToastService } from '../../core/services/toast.service';
import { LogEntry, LogFilters, LOG_EVENT_TYPE_OPTIONS } from '../../core/models/log.models';
import { displayChannelLabel, displayMemberLabel } from '../../core/models/ticket.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-logs',
  templateUrl: './logs.component.html',
  styleUrls: ['./logs.component.css']
})
export class LogsComponent implements OnInit, OnDestroy {
  guildId = '';
  logs: LogEntry[] = [];
  loading = true;
  error = '';
  typeOptions = LOG_EVENT_TYPE_OPTIONS;
  canManageSettings = false;
  showClearDialog = false;
  clearConfirmation = '';
  clearing = false;

  filters: LogFilters = {
    type: '',
    from: '',
    to: '',
    search: '',
    userId: ''
  };

  private accessSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private guildService: GuildService,
    private guildContext: GuildContextService,
    private guildAccessService: GuildAccessService,
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.guildId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.guildId) {
      this.router.navigate(['/servers']);
      return;
    }

    this.guildContext.ensureGuild(this.guildId, this.guildService);
    this.accessSub = this.guildAccessService.loadAccess(this.guildId).subscribe({
      next: access => { this.canManageSettings = !!access.canManageSettings; },
      error: () => { this.canManageSettings = false; }
    });
    this.loadLogs();
  }

  ngOnDestroy(): void {
    this.accessSub?.unsubscribe();
  }

  get canConfirmClear(): boolean {
    return this.clearConfirmation.trim() === 'DELETE';
  }

  loadLogs(): void {
    this.loading = true;
    this.error = '';

    this.guildService.getLogs(this.guildId, this.buildActiveFilters()).subscribe({
      next: logs => {
        this.logs = logs;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('errors.loadLogs'));
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.loadLogs();
  }

  clearFilters(): void {
    this.filters = { type: '', from: '', to: '', search: '', userId: '' };
    this.loadLogs();
  }

  openClearDialog(): void {
    this.clearConfirmation = '';
    this.showClearDialog = true;
  }

  closeClearDialog(): void {
    if (this.clearing) {
      return;
    }

    this.showClearDialog = false;
    this.clearConfirmation = '';
  }

  clearAllLogs(): void {
    if (!this.canConfirmClear || this.clearing) {
      return;
    }

    this.clearing = true;

    this.guildService.clearLogs(this.guildId, this.clearConfirmation.trim()).subscribe({
      next: result => {
        this.clearing = false;
        this.showClearDialog = false;
        this.clearConfirmation = '';
        this.toast.success(
          this.translate.instant('logs.clearSuccess', { count: result.deletedCount })
        );
        this.loadLogs();
      },
      error: err => {
        this.clearing = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('logs.clearError')));
      }
    });
  }

  displayValue(value?: string | null): string {
    return value?.trim() ? value : this.translate.instant('common.emptyValue');
  }

  displayMember = displayMemberLabel;
  displayChannel = displayChannelLabel;

  private buildActiveFilters(): LogFilters {
    return {
      type: this.filters.type || undefined,
      from: this.filters.from || undefined,
      to: this.filters.to || undefined,
      search: this.filters.search?.trim() || undefined,
      userId: this.filters.userId?.trim() || undefined
    };
  }
}
