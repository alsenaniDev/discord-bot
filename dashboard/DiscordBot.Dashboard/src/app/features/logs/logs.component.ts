import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildAccessService } from '../../core/services/guild-access.service';
import { ToastService } from '../../core/services/toast.service';
import { LogEntry, LogEventType, LogFilters } from '../../core/models/log.models';
import { displayChannelLabel, displayMemberLabel } from '../../core/models/ticket.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroStat
} from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';
import { StatusBadgeTone } from '../../shared/ui/status-badge/status-badge.component';
import { LogsDetailPanelComponent } from './logs-detail-panel/logs-detail-panel.component';

@Component({
  selector: 'app-logs',
  templateUrl: './logs.component.html',
  styleUrls: ['./logs.component.css']
})
export class LogsComponent implements OnInit, OnDestroy {
  @ViewChild('logDetailPanel') logDetailPanel?: LogsDetailPanelComponent;

  guildId = '';
  logs: LogEntry[] = [];
  loading = true;
  error = '';
  canManageSettings = false;
  canAccessLogs = true;
  canClearLogs = false;
  logsEnabled = false;
  logChannelConfigured = false;
  showClearDialog = false;
  clearConfirmation = '';
  clearing = false;
  selectedLogId = '';

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
      next: access => {
        this.canManageSettings = !!access.canManageSettings;
        this.canAccessLogs = !!access.canAccessLogs;
        this.canClearLogs = !!(access.canClearLogs ?? access.canManageSettings);

        if (this.canAccessLogs) {
          this.loadLogs();
          this.loadDeliveryHint();
        } else {
          this.loading = false;
        }
      },
      error: () => {
        this.canManageSettings = false;
        this.canAccessLogs = false;
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.accessSub?.unsubscribe();
  }

  get canConfirmClear(): boolean {
    return this.clearConfirmation.trim() === 'DELETE';
  }

  get hasActiveFilters(): boolean {
    return !!(
      this.filters.type ||
      this.filters.from ||
      this.filters.to ||
      this.filters.search?.trim() ||
      this.filters.userId?.trim()
    );
  }

  get selectedLog(): LogEntry | null {
    return this.logs.find(log => log.id === this.selectedLogId) ?? null;
  }

  get todayCount(): number {
    const start = new Date();
    start.setHours(0, 0, 0, 0);
    return this.logs.filter(log => new Date(log.createdAt) >= start).length;
  }

  get warningCount(): number {
    return this.logs.filter(log => this.severityTone(log.type) === 'warning').length;
  }

  get errorCount(): number {
    return this.logs.filter(log => this.severityTone(log.type) === 'danger').length;
  }

  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    return [
      {
        label: this.translate.instant('logs.workspace.stats.total'),
        value: String(this.logs.length)
      },
      {
        label: this.translate.instant('logs.workspace.stats.today'),
        value: String(this.todayCount)
      },
      {
        label: this.translate.instant('logs.workspace.stats.warnings'),
        value: String(this.warningCount)
      },
      {
        label: this.translate.instant('logs.workspace.stats.errors'),
        value: String(this.errorCount)
      },
      {
        label: this.translate.instant('logs.workspace.stats.discordDelivery'),
        value: this.discordDeliveryLabel,
        compact: true
      }
    ];
  }

  get discordDeliveryLabel(): string {
    if (!this.logsEnabled) {
      return this.translate.instant('logs.workspace.delivery.disabled');
    }

    if (this.logChannelConfigured) {
      return this.translate.instant('logs.workspace.delivery.configured');
    }

    return this.translate.instant('logs.workspace.delivery.incomplete');
  }

  get workspaceHeroFooter(): string {
    return this.translate.instant('logs.workspace.footer');
  }

  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction | null {
    if (!this.canManageSettings) {
      return null;
    }

    return {
      label: this.translate.instant('logs.workspace.cta.configure')
    };
  }

  loadLogs(): void {
    this.loading = true;
    this.error = '';

    this.guildService.getLogs(this.guildId, this.buildActiveFilters()).subscribe({
      next: logs => {
        this.logs = logs;
        this.loading = false;

        if (this.selectedLogId && !this.logs.some(log => log.id === this.selectedLogId)) {
          this.selectedLogId = '';
        }
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('logs.loadError'));
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.selectedLogId = '';
    this.loadLogs();
  }

  clearFilters(): void {
    this.filters = { type: '', from: '', to: '', search: '', userId: '' };
    this.selectedLogId = '';
    this.loadLogs();
  }

  configureLogs(): void {
    this.router.navigate(['/guilds', this.guildId, 'settings']);
  }

  selectLog(log: LogEntry): void {
    if (this.selectedLogId === log.id) {
      return;
    }

    this.selectedLogId = log.id;
    setTimeout(() => {
      this.logDetailPanel?.focusTitle();
      document.getElementById(`log-${log.id}`)?.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest'
      });
    });
  }

  closeDetail(): void {
    this.selectedLogId = '';
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
        this.selectedLogId = '';
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

  actorLabel(log: LogEntry): string {
    if (!log.actorDiscordUserId && !log.actorDisplayName) {
      return this.displayValue(null);
    }

    return this.displayMember(log.actorDisplayName, log.actorDiscordUserId);
  }

  targetLabel(log: LogEntry): string {
    if (!log.targetDiscordUserId && !log.targetDisplayName) {
      return this.displayValue(null);
    }

    return this.displayMember(log.targetDisplayName, log.targetDiscordUserId);
  }

  channelLabel(log: LogEntry): string {
    if (!log.channelDiscordId && !log.channelName) {
      return this.displayValue(null);
    }

    return this.displayChannel(log.channelName, log.channelDiscordId);
  }

  severityTone(type: LogEventType): StatusBadgeTone {
    switch (type) {
      case 'MemberKicked':
      case 'MessagesCleared':
      case 'ReactionRoleDeleted':
        return 'danger';
      case 'WarningCreated':
      case 'ModuleChanged':
        return 'warning';
      case 'MemberJoined':
      case 'WelcomeSent':
      case 'AutoRoleAssigned':
      case 'TicketOpened':
      case 'ResourceSyncCompleted':
      case 'ReactionRoleAssigned':
      case 'ReactionRoleCreated':
        return 'success';
      case 'SettingsUpdated':
      case 'TicketClosed':
      case 'TicketArchived':
      case 'ReactionRoleRemoved':
        return 'info';
      default:
        return 'neutral';
    }
  }

  severityLabel(type: LogEventType): string {
    const tone = this.severityTone(type);
    return this.translate.instant(`logs.workspace.severity.${tone}`);
  }

  eventIcon(type: LogEventType): string {
    switch (type) {
      case 'TicketOpened':
      case 'TicketClosed':
      case 'TicketArchived':
        return 'tickets';
      case 'WarningCreated':
      case 'MemberKicked':
      case 'MessagesCleared':
        return 'shield';
      case 'ModuleChanged':
      case 'SettingsUpdated':
        return 'settings';
      case 'MemberJoined':
      case 'WelcomeSent':
      case 'AutoRoleAssigned':
        return 'check-circle';
      default:
        return 'logs';
    }
  }

  metadataPreview(log: LogEntry): string {
    const raw = log.metadataJson?.trim();
    if (!raw) {
      return '';
    }

    try {
      return JSON.stringify(JSON.parse(raw), null, 2);
    } catch {
      return raw;
    }
  }

  isSelected(log: LogEntry): boolean {
    return this.selectedLogId === log.id;
  }

  private loadDeliveryHint(): void {
    this.guildService.getSettings(this.guildId).subscribe({
      next: settings => {
        this.logsEnabled = !!settings.logsEnabled;
        this.logChannelConfigured = !!(settings.logChannelId?.trim());
      },
      error: () => {
        this.logsEnabled = false;
        this.logChannelConfigured = false;
      }
    });
  }

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
