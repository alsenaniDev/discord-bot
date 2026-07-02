import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { LogEntry, LogFilters, LOG_EVENT_TYPE_OPTIONS } from '../../core/models/log.models';
import { displayChannelLabel, displayMemberLabel } from '../../core/models/ticket.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-logs',
  templateUrl: './logs.component.html',
  styleUrls: ['./logs.component.css']
})
export class LogsComponent implements OnInit {
  guildId = '';
  logs: LogEntry[] = [];
  loading = true;
  error = '';
  typeOptions = LOG_EVENT_TYPE_OPTIONS;

  filters: LogFilters = {
    type: '',
    from: '',
    to: '',
    search: '',
    userId: ''
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private guildService: GuildService,
    private guildContext: GuildContextService,
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
    this.loadLogs();
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
