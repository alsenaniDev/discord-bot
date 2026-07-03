import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin, Subscription } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildAccessService } from '../../core/services/guild-access.service';
import { ToastService } from '../../core/services/toast.service';
import { ReactionRolePanel } from '../../core/models/reaction-role.models';
import { DiscordChannel, DiscordRole, channelLabel, roleLabel } from '../../core/models/guild.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroStat
} from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';
import {
  ReactionRolesDetailPanelComponent
} from './reaction-roles-detail-panel/reaction-roles-detail-panel.component';
import { ReactionRolesUiFilters } from './reaction-roles-filter-bar/reaction-roles-filter-bar.component';

@Component({
  selector: 'app-reaction-roles',
  templateUrl: './reaction-roles.component.html',
  styleUrls: ['./reaction-roles.component.css']
})
export class ReactionRolesComponent implements OnInit, OnDestroy {
  @ViewChild('panelDetail') panelDetail?: ReactionRolesDetailPanelComponent;

  guildId = '';
  discordGuildId = '';
  panels: ReactionRolePanel[] = [];
  channels: DiscordChannel[] = [];
  roles: DiscordRole[] = [];
  loading = true;
  error = '';
  canAccessReactionRoles = true;
  deactivatingId: string | null = null;
  selectedPanelId = '';
  uiFilters: ReactionRolesUiFilters = {
    search: '',
    status: 'all',
    channel: ''
  };

  private accessSub?: Subscription;
  private guildSub?: Subscription;

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
    this.guildSub = this.guildContext.selectedGuild$.subscribe(guild => {
      this.discordGuildId = guild?.discordGuildId ?? '';
    });

    this.accessSub = this.guildAccessService.loadAccess(this.guildId).subscribe({
      next: access => {
        this.canAccessReactionRoles = !!(access.isOwner || access.isPlatformAdmin);
        if (this.canAccessReactionRoles) {
          this.loadData();
        } else {
          this.loading = false;
        }
      },
      error: () => {
        this.canAccessReactionRoles = false;
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.accessSub?.unsubscribe();
    this.guildSub?.unsubscribe();
  }

  get filteredPanels(): ReactionRolePanel[] {
    return this.panels.filter(panel => this.matchesFilters(panel));
  }

  get selectedPanel(): ReactionRolePanel | null {
    return this.panels.find(panel => panel.id === this.selectedPanelId) ?? null;
  }

  get hasActiveFilters(): boolean {
    return !!(
      this.uiFilters.search.trim() ||
      this.uiFilters.status !== 'all' ||
      this.uiFilters.channel
    );
  }

  get channelFilterOptions(): { id: string; label: string }[] {
    const ids = [...new Set(this.panels.map(panel => panel.channelDiscordId).filter(Boolean))];
    return ids
      .map(id => ({ id, label: this.channelName(id) }))
      .sort((a, b) => a.label.localeCompare(b.label));
  }

  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    const activePanels = this.panels.filter(panel => panel.isActive).length;
    const rolesAssigned = new Set(this.panels.map(panel => panel.roleDiscordId).filter(Boolean)).size;
    const messagesLinked = this.panels.filter(panel => this.hasLinkedMessage(panel)).length;

    return [
      {
        label: this.translate.instant('workspaceHero.reactionRoles.stats.panels'),
        value: String(this.panels.length)
      },
      {
        label: this.translate.instant('workspaceHero.reactionRoles.stats.active'),
        value: String(activePanels)
      },
      {
        label: this.translate.instant('workspaceHero.reactionRoles.stats.rolesAssigned'),
        value: String(rolesAssigned)
      },
      {
        label: this.translate.instant('workspaceHero.reactionRoles.stats.messagesLinked'),
        value: String(messagesLinked)
      }
    ];
  }

  get workspaceHeroFooter(): string {
    return this.translate.instant('workspaceHero.reactionRoles.footer');
  }

  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction {
    return {
      label: this.translate.instant('workspaceHero.reactionRoles.cta.create')
    };
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    forkJoin({
      panels: this.guildService.getReactionRoles(this.guildId),
      channels: this.guildService.getChannels(this.guildId),
      roles: this.guildService.getRoles(this.guildId)
    }).subscribe({
      next: ({ panels, channels, roles }) => {
        this.panels = panels;
        this.channels = channels;
        this.roles = roles;
        this.loading = false;

        if (this.selectedPanelId && !this.panels.some(panel => panel.id === this.selectedPanelId)) {
          this.selectedPanelId = '';
        }
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('reactionRoles.loadError'));
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.selectedPanelId = '';
  }

  clearFilters(): void {
    this.uiFilters = { search: '', status: 'all', channel: '' };
    this.selectedPanelId = '';
  }

  onHeroPrimaryAction(): void {
    document.getElementById('reaction-roles-panels')?.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });
  }

  onEditPanel(): void {
    this.toast.success(this.translate.instant('reactionRoles.workspace.editHint'));
  }

  selectPanel(panel: ReactionRolePanel): void {
    if (this.selectedPanelId === panel.id) {
      return;
    }

    this.selectedPanelId = panel.id;
    setTimeout(() => {
      this.panelDetail?.focusTitle();
      document.getElementById(`reaction-role-panel-${panel.id}`)?.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest'
      });
    });
  }

  closeDetail(): void {
    this.selectedPanelId = '';
  }

  isSelected(panel: ReactionRolePanel): boolean {
    return this.selectedPanelId === panel.id;
  }

  channelName(channelId: string): string {
    const channel = this.channels.find(item => item.discordChannelId === channelId);
    return channel ? channelLabel(channel) : channelId;
  }

  roleName(roleId: string): string {
    const role = this.roles.find(item => item.discordRoleId === roleId);
    return role ? roleLabel(role) : roleId;
  }

  hasLinkedMessage(panel: ReactionRolePanel): boolean {
    return !!panel.messageDiscordId?.trim();
  }

  messageStatusLabel(panel: ReactionRolePanel): string {
    return this.hasLinkedMessage(panel)
      ? this.translate.instant('reactionRoles.workspace.messageLinked')
      : this.translate.instant('reactionRoles.workspace.messageMissing');
  }

  canOpenPanel(panel: ReactionRolePanel): boolean {
    return this.hasLinkedMessage(panel) && !!this.discordGuildId && !!panel.channelDiscordId;
  }

  discordMessageUrl(panel: ReactionRolePanel): string | null {
    if (!this.canOpenPanel(panel)) {
      return null;
    }

    return `https://discord.com/channels/${this.discordGuildId}/${panel.channelDiscordId}/${panel.messageDiscordId}`;
  }

  openInDiscord(panel: ReactionRolePanel): void {
    const url = this.discordMessageUrl(panel);
    if (!url) {
      return;
    }

    window.open(url, '_blank', 'noopener,noreferrer');
  }

  async copyPanelLink(panel: ReactionRolePanel): Promise<void> {
    const url = this.discordMessageUrl(panel);
    const value = url ?? panel.messageDiscordId;

    if (!value) {
      return;
    }

    try {
      await navigator.clipboard.writeText(value);
      this.toast.success(this.translate.instant('reactionRoles.workspace.copySuccess'));
    } catch {
      this.toast.error(this.translate.instant('reactionRoles.workspace.copyError'));
    }
  }

  deactivate(panel: ReactionRolePanel): void {
    if (!panel.isActive || this.deactivatingId) {
      return;
    }

    this.deactivatingId = panel.id;

    this.guildService.deactivateReactionRole(this.guildId, panel.id).subscribe({
      next: () => {
        panel.isActive = false;
        this.deactivatingId = null;
        this.toast.success(
          this.translate.instant('reactionRoles.deactivatedWithTitle', { title: panel.title })
        );
      },
      error: err => {
        this.deactivatingId = null;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('reactionRoles.deactivateError')));
      }
    });
  }

  isDeactivating(panel: ReactionRolePanel): boolean {
    return this.deactivatingId === panel.id;
  }

  private matchesFilters(panel: ReactionRolePanel): boolean {
    if (this.uiFilters.status === 'active' && !panel.isActive) {
      return false;
    }

    if (this.uiFilters.status === 'inactive' && panel.isActive) {
      return false;
    }

    if (this.uiFilters.channel && panel.channelDiscordId !== this.uiFilters.channel) {
      return false;
    }

    const query = this.uiFilters.search.trim().toLowerCase();
    if (!query) {
      return true;
    }

    const haystack = [
      panel.title,
      panel.description,
      panel.buttonLabel,
      this.channelName(panel.channelDiscordId),
      this.roleName(panel.roleDiscordId)
    ]
      .join(' ')
      .toLowerCase();

    return haystack.includes(query);
  }
}
