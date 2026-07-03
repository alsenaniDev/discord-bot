import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin, Subscription } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildAccessService } from '../../core/services/guild-access.service';
import { ToastService } from '../../core/services/toast.service';
import {
  ModerationCase,
  ModerationFilters,
  Warning,
  displayMemberLabel,
} from '../../core/models/moderation.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroStat
} from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';
import { StatusBadgeTone } from '../../shared/ui/status-badge/status-badge.component';
import { ModerationDetailPanelComponent } from './moderation-detail-panel/moderation-detail-panel.component';

interface ModerationFeedItem {
  key: string;
  actionType: string;
  actionLabelKey: string;
  badgeTone: StatusBadgeTone;
  iconTone: 'success' | 'warning' | 'danger' | 'info';
  targetUserId?: string | null;
  targetDisplayName?: string | null;
  moderatorUserId: string;
  moderatorDisplayName?: string | null;
  reason: string;
  createdAt: string;
  messageCount?: number | null;
  channelDiscordId?: string | null;
  channelName?: string | null;
}

@Component({
  selector: 'app-moderation',
  templateUrl: './moderation.component.html',
  styleUrls: ['./moderation.component.css']
})
export class ModerationComponent implements OnInit, OnDestroy {
  @ViewChild('moderationDetailPanel') moderationDetailPanel?: ModerationDetailPanelComponent;

  guildId = '';
  warnings: Warning[] = [];
  cases: ModerationCase[] = [];
  loading = true;
  error = '';
  canAccessModeration = true;
  selectedFeedKey = '';
  uiSearch = '';
  uiModeratorUserId = '';

  filters: ModerationFilters = {
    targetUserId: '',
    type: '',
    from: '',
    to: ''
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
        this.canAccessModeration = !!access.canAccessModeration;
        if (this.canAccessModeration) {
          this.loadData();
        } else {
          this.loading = false;
        }
      },
      error: () => {
        this.canAccessModeration = false;
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.accessSub?.unsubscribe();
  }

  get feedItems(): ModerationFeedItem[] {
    const items: ModerationFeedItem[] = [
      ...this.warnings.map(warning => this.warningToFeedItem(warning)),
      ...this.cases.map(item => this.caseToFeedItem(item))
    ];

    return items
      .filter(item => this.matchesClientFilters(item))
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  }

  get selectedFeedItem(): ModerationFeedItem | null {
    return this.feedItems.find(item => item.key === this.selectedFeedKey) ?? null;
  }

  get hasActiveFilters(): boolean {
    return !!(
      this.filters.type ||
      this.filters.from ||
      this.filters.to ||
      this.filters.targetUserId?.trim() ||
      this.uiModeratorUserId.trim() ||
      this.uiSearch.trim()
    );
  }

  get todayStart(): Date {
    const start = new Date();
    start.setHours(0, 0, 0, 0);
    return start;
  }

  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    return [
      {
        label: this.translate.instant('moderation.workspace.stats.active'),
        value: String(this.warnings.length + this.cases.length)
      },
      {
        label: this.translate.instant('moderation.workspace.stats.warningsToday'),
        value: String(this.countToday(item => item.actionType === 'Warn'))
      },
      {
        label: this.translate.instant('moderation.workspace.stats.timeoutsToday'),
        value: String(this.countToday(item => item.actionType === 'Timeout'))
      },
      {
        label: this.translate.instant('moderation.workspace.stats.bansToday'),
        value: String(this.countToday(item => item.actionType === 'Ban' || item.actionType === 'Kick'))
      }
    ];
  }

  get workspaceHeroFooter(): string {
    return this.translate.instant('moderation.workspace.footer');
  }

  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction | null {
    return {
      label: this.translate.instant('moderation.workspace.cta.settings')
    };
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    const activeFilters = this.buildActiveFilters();

    forkJoin({
      warnings: this.guildService.getWarnings(this.guildId, activeFilters),
      cases: this.guildService.getModerationCases(this.guildId, activeFilters)
    }).subscribe({
      next: ({ warnings, cases }) => {
        this.warnings = warnings;
        this.cases = cases;
        this.loading = false;

        if (this.selectedFeedKey && !this.feedItems.some(item => item.key === this.selectedFeedKey)) {
          this.selectedFeedKey = '';
        }
      },
      error: err => {
        this.loading = false;
        const message = getApiErrorMessage(err, this.translate.instant('moderation.loadError'));
        this.error = message;
        this.toast.error(message);
      }
    });
  }

  applyFilters(): void {
    this.selectedFeedKey = '';
    this.loadData();
  }

  clearFilters(): void {
    this.filters = { targetUserId: '', type: '', from: '', to: '' };
    this.uiSearch = '';
    this.uiModeratorUserId = '';
    this.selectedFeedKey = '';
    this.loadData();
  }

  openModerationSettings(): void {
    this.router.navigate(['/guilds', this.guildId, 'moderation', 'settings']);
  }

  selectFeedItem(item: ModerationFeedItem): void {
    if (this.selectedFeedKey === item.key) {
      return;
    }

    this.selectedFeedKey = item.key;
    setTimeout(() => {
      this.moderationDetailPanel?.focusTitle();
      document.getElementById(`moderation-${item.key}`)?.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest'
      });
    });
  }

  closeDetail(): void {
    this.selectedFeedKey = '';
  }

  isSelected(item: ModerationFeedItem): boolean {
    return this.selectedFeedKey === item.key;
  }

  actionLabel(item: ModerationFeedItem): string {
    return this.translate.instant(item.actionLabelKey);
  }

  targetLabel(item: ModerationFeedItem): string {
    return this.displayMember(item.targetDisplayName, item.targetUserId);
  }

  moderatorLabel(item: ModerationFeedItem): string {
    return this.displayMember(item.moderatorDisplayName, item.moderatorUserId);
  }

  evidenceLabel(item: ModerationFeedItem): string {
    const parts: string[] = [];

    if (item.messageCount) {
      parts.push(this.translate.instant('moderation.workspace.messageCountValue', { count: item.messageCount }));
    }

    if (item.channelDiscordId || item.channelName) {
      parts.push(this.displayChannel(item.channelName, item.channelDiscordId));
    }

    return parts.length > 0 ? parts.join(' · ') : this.displayValue(null);
  }

  durationLabel(): string {
    return this.displayValue(null);
  }

  messageCountLabel(item: ModerationFeedItem): string {
    if (!item.messageCount) {
      return '';
    }

    return this.translate.instant('moderation.workspace.messageCountValue', { count: item.messageCount });
  }

  displayMember(name?: string | null, id?: string | null): string {
    return displayMemberLabel(name, id);
  }

  displayChannel(name?: string | null, id?: string | null): string {
    return name?.trim() ? `#${name.trim()}` : id?.trim() ? id.trim() : this.translate.instant('common.emptyValue');
  }

  displayValue(value?: string | null): string {
    return value?.trim() ? value : this.translate.instant('common.emptyValue');
  }

  private warningToFeedItem(warning: Warning): ModerationFeedItem {
    return {
      key: `warning-${warning.id}`,
      actionType: 'Warn',
      actionLabelKey: 'moderation.workspace.actions.warning',
      badgeTone: 'warning',
      iconTone: 'warning',
      targetUserId: warning.targetDiscordUserId,
      targetDisplayName: warning.targetDisplayName,
      moderatorUserId: warning.moderatorDiscordUserId,
      moderatorDisplayName: warning.moderatorDisplayName,
      reason: warning.reason,
      createdAt: warning.createdAt
    };
  }

  private caseToFeedItem(item: ModerationCase): ModerationFeedItem {
    const actionType = this.normalizeCaseType(item.type);
    const badgeTone = this.actionBadgeTone(actionType);
    const iconTone = this.actionIconTone(badgeTone);

    return {
      key: `case-${item.id}`,
      actionType,
      actionLabelKey: this.actionLabelKey(actionType),
      badgeTone,
      iconTone,
      targetUserId: item.targetDiscordUserId,
      targetDisplayName: item.targetDisplayName,
      moderatorUserId: item.moderatorDiscordUserId,
      moderatorDisplayName: item.moderatorDisplayName,
      reason: item.reason?.trim() || this.translate.instant('common.emptyValue'),
      createdAt: item.createdAt,
      messageCount: item.messageCount,
      channelDiscordId: item.channelDiscordId,
      channelName: item.channelName
    };
  }

  private actionLabelKey(actionType: string): string {
    switch (actionType) {
      case 'Warn':
        return 'moderation.workspace.actions.warning';
      case 'Kick':
        return 'moderation.workspace.actions.kick';
      case 'Clear':
        return 'moderation.workspace.actions.mute';
      case 'Timeout':
        return 'moderation.workspace.actions.timeout';
      case 'Ban':
        return 'moderation.workspace.actions.ban';
      case 'Unban':
        return 'moderation.workspace.actions.unban';
      default:
        return 'moderation.warn';
    }
  }

  private normalizeCaseType(type: number | string): string {
    if (type === 0 || type === 'Warn') {
      return 'Warn';
    }

    if (type === 1 || type === 'Kick') {
      return 'Kick';
    }

    if (type === 2 || type === 'Clear') {
      return 'Clear';
    }

    return String(type);
  }

  private actionBadgeTone(actionType: string): StatusBadgeTone {
    switch (actionType) {
      case 'Warn':
        return 'warning';
      case 'Kick':
        return 'danger';
      case 'Clear':
        return 'info';
      case 'Timeout':
        return 'info';
      case 'Ban':
        return 'danger';
      case 'Unban':
        return 'success';
      case 'Mute':
        return 'neutral';
      default:
        return 'neutral';
    }
  }

  private actionIconTone(tone: StatusBadgeTone): 'success' | 'warning' | 'danger' | 'info' {
    if (tone === 'danger') {
      return 'danger';
    }

    if (tone === 'warning') {
      return 'warning';
    }

    if (tone === 'success') {
      return 'success';
    }

    return 'info';
  }

  private matchesClientFilters(item: ModerationFeedItem): boolean {
    if (this.uiModeratorUserId.trim() && item.moderatorUserId !== this.uiModeratorUserId.trim()) {
      return false;
    }

    const query = this.uiSearch.trim().toLowerCase();
    if (!query) {
      return true;
    }

    const haystack = [
      item.reason,
      item.targetDisplayName,
      item.targetUserId,
      item.moderatorDisplayName,
      item.moderatorUserId,
      this.translate.instant(item.actionLabelKey)
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();

    return haystack.includes(query);
  }

  private countToday(predicate: (item: ModerationFeedItem) => boolean): number {
    const allItems = [
      ...this.warnings.map(warning => this.warningToFeedItem(warning)),
      ...this.cases.map(item => this.caseToFeedItem(item))
    ];

    return allItems.filter(item => predicate(item) && new Date(item.createdAt) >= this.todayStart).length;
  }

  private buildActiveFilters(): ModerationFilters {
    return {
      targetUserId: this.filters.targetUserId?.trim() || undefined,
      type: this.filters.type || undefined,
      from: this.filters.from || undefined,
      to: this.filters.to || undefined
    };
  }
}
