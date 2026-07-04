import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin, Subscription } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { AuthService } from '../../core/services/auth.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import {
  DiscordChannel,
  DiscordRole,
  channelLabel,
  isAssignableRole,
  isCategoryChannel,
  isTextChannel,
  roleLabel
} from '../../core/models/guild.models';
import {
  COMMAND_PANEL_ACTIONS,
  COMMAND_PANEL_STYLES,
  CommandPanelButton,
  DEFAULT_COMMAND_PANEL_BUTTONS
} from '../../core/models/command-panel.models';
import {
  AUTO_REPLY_MATCH_MODES,
  AUTO_REPLY_SCOPES,
  AutoReplyRule,
  AutoReplyMatchMode,
  AutoReplyScope,
  CreateAutoReplyRule
} from '../../core/models/auto-reply.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import { requiredWhenEnabled, optionalHttpUrlValidator, optionalSnowflakeValidator } from '../../core/utils/settings.validators';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroIconName,
  PageWorkspaceHeroStat
} from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';
import { AutoRolePermissionStatus } from '../auto-role/auto-role-editor/auto-role-editor.component';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit, OnDestroy {
  guildId = '';
  form!: FormGroup;
  loading = true;
  saving = false;
  syncing = false;
  error = '';

  textChannels: DiscordChannel[] = [];
  categories: DiscordChannel[] = [];
  roles: DiscordRole[] = [];
  assignableRoles: DiscordRole[] = [];
  ticketsEnabled = false;
  panelButtons: CommandPanelButton[] = [];
  autoReplies: AutoReplyRule[] = [];
  autoReplyLoading = false;
  autoReplySaving = false;
  editingAutoReplyId = '';
  activeTab: SettingsTabId = 'general';
  guildName = '';
  guildIconUrl: string | null = null;
  readonly welcomeVariables = ['{user}', '{server}', '{memberCount}', '{username}', '{mention}'];
  private guildSub?: Subscription;
  autoReplyForm = {
    trigger: '',
    response: '',
    matchMode: 'Contains' as AutoReplyMatchMode,
    scope: 'AllChannels' as AutoReplyScope,
    enabled: true,
    priority: 0
  };

  readonly panelActions = COMMAND_PANEL_ACTIONS;
  readonly panelStyles = COMMAND_PANEL_STYLES;
  readonly autoReplyMatchModes = AUTO_REPLY_MATCH_MODES;
  readonly autoReplyScopes = AUTO_REPLY_SCOPES;

  readonly settingsTabs: SettingsTab[] = [
    { id: 'general', labelKey: 'settings.tabs.general' },
    { id: 'welcome', labelKey: 'settings.tabs.welcome' },
    { id: 'autoRole', labelKey: 'settings.tabs.autoRole' },
    { id: 'logs', labelKey: 'settings.tabs.logs' },
    { id: 'tickets', labelKey: 'settings.tabs.tickets', requiresTickets: true },
    { id: 'autoReplies', labelKey: 'settings.tabs.autoReplies' }
  ];

  channelLabel = channelLabel;
  roleLabel = roleLabel;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private guildService: GuildService,
    private auth: AuthService,
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

    this.form = this.fb.group({
      welcomeEnabled: [false],
      welcomeChannelId: ['', [requiredWhenEnabled('welcomeEnabled')]],
      welcomeMessage: ['Welcome {user} to {server}!', [Validators.required, Validators.maxLength(2000)]],
      autoRoleEnabled: [false],
      autoRoleId: ['', [requiredWhenEnabled('autoRoleEnabled')]],
      logsEnabled: [false],
      logChannelId: ['', [requiredWhenEnabled('logsEnabled')]],
      ticketCategoryId: [''],
      ticketArchiveChannelId: ['', [optionalSnowflakeValidator()]],
      ticketWelcomeTitle: ['Ticket #{ticket}', [Validators.required, Validators.maxLength(256)]],
      ticketWelcomeMessage: [
        '{mention}, thanks for reaching out.\n\nA staff member will assist you shortly. Use the **Close ticket** button when your issue is resolved.',
        [Validators.required, Validators.maxLength(2000)]
      ],
      ticketClosedMessage: ['Ticket #{ticket} was closed by {mention}.', [Validators.required, Validators.maxLength(2000)]],
      ticketClosedFromDashboardMessage: [
        'Ticket #{ticket} was closed from the dashboard. This channel will be deleted shortly.',
        [Validators.required, Validators.maxLength(2000)]
      ],
      ticketStaffReplyPrefix: ['**{staff}** replied from the dashboard:', [Validators.required, Validators.maxLength(2000)]],
      commandPanelEnabled: [false],
      commandPanelChannelId: ['', [requiredWhenEnabled('commandPanelEnabled')]],
      commandPanelTitle: ['How can we help?', [Validators.required, Validators.maxLength(256)]],
      commandPanelDescription: [
        'Use the buttons below — no commands needed.',
        [Validators.required, Validators.maxLength(2000)]
      ],
      commandPanelImageUrl: ['', [Validators.maxLength(2048), optionalHttpUrlValidator()]]
    });

    this.form.get('welcomeEnabled')?.valueChanges.subscribe(() => this.form.get('welcomeChannelId')?.updateValueAndValidity());
    this.form.get('autoRoleEnabled')?.valueChanges.subscribe(() => this.form.get('autoRoleId')?.updateValueAndValidity());
    this.form.get('logsEnabled')?.valueChanges.subscribe(() => this.form.get('logChannelId')?.updateValueAndValidity());
    this.form.get('commandPanelEnabled')?.valueChanges.subscribe(() =>
      this.form.get('commandPanelChannelId')?.updateValueAndValidity()
    );

    this.loadPageData();
    this.loadAutoReplies();

    this.guildSub = this.guildContext.selectedGuild$.subscribe(guild => {
      this.guildName = guild?.name ?? '';
      this.guildIconUrl = guild?.iconUrl ?? null;
    });
  }

  ngOnDestroy(): void {
    this.guildSub?.unsubscribe();
  }

  loadAutoReplies(): void {
    this.autoReplyLoading = true;
    this.guildService.getAutoReplies(this.guildId).subscribe({
      next: rules => {
        this.autoReplies = rules;
        this.autoReplyLoading = false;
      },
      error: err => {
        this.autoReplyLoading = false;
        if (err.status !== 401) {
          this.toast.error(getApiErrorMessage(err, this.translate.instant('settings.autoReplies.loadError')));
        }
      }
    });
  }

  loadPageData(): void {
    this.loading = true;
    this.error = '';

    forkJoin({
      settings: this.guildService.getSettings(this.guildId),
      channels: this.guildService.getChannels(this.guildId),
      categories: this.guildService.getCategories(this.guildId),
      roles: this.guildService.getRoles(this.guildId)
    }).subscribe({
      next: ({ settings, channels, categories, roles }) => {
        this.ticketsEnabled = settings.ticketsEnabled;
        if (this.activeTab === 'tickets' && !settings.ticketsEnabled) {
          this.activeTab = 'general';
        }
        this.textChannels = channels.filter(isTextChannel);
        this.categories = categories.length > 0 ? categories : channels.filter(isCategoryChannel);
        this.roles = roles;
        this.assignableRoles = roles.filter(isAssignableRole);
        this.panelButtons = this.normalizePanelButtons(settings.commandPanelButtons);

        this.form.patchValue({
          welcomeEnabled: settings.welcomeEnabled,
          welcomeChannelId: settings.welcomeChannelId ?? '',
          welcomeMessage: settings.welcomeMessage,
          autoRoleEnabled: settings.autoRoleEnabled,
          autoRoleId: settings.autoRoleId ?? '',
          logsEnabled: settings.logsEnabled,
          logChannelId: settings.logChannelId ?? '',
          ticketCategoryId: settings.ticketCategoryId ?? '',
          ticketArchiveChannelId: settings.ticketArchiveChannelId ?? '',
          ticketWelcomeTitle: settings.ticketWelcomeTitle || 'Ticket #{ticket}',
          ticketWelcomeMessage:
            settings.ticketWelcomeMessage ||
            '{mention}, thanks for reaching out.\n\nA staff member will assist you shortly. Use the **Close ticket** button when your issue is resolved.',
          ticketClosedMessage: settings.ticketClosedMessage || 'Ticket #{ticket} was closed by {mention}.',
          ticketClosedFromDashboardMessage:
            settings.ticketClosedFromDashboardMessage ||
            'Ticket #{ticket} was closed from the dashboard. This channel will be deleted shortly.',
          ticketStaffReplyPrefix: settings.ticketStaffReplyPrefix || '**{staff}** replied from the dashboard:',
          commandPanelEnabled: settings.commandPanelEnabled,
          commandPanelChannelId: settings.commandPanelChannelId ?? '',
          commandPanelTitle: settings.commandPanelTitle,
          commandPanelDescription: settings.commandPanelDescription,
          commandPanelImageUrl: settings.commandPanelImageUrl ?? ''
        });

        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err.status === 401) {
          this.handleAuthError();
        } else {
          const message = getApiErrorMessage(err, this.translate.instant('errors.loadSettingsAccess'));
          this.error = message;
          this.toast.error(message);
        }
      }
    });
  }

  syncDiscordData(): void {
    this.syncing = true;

    this.guildService.requestResourceSync(this.guildId).subscribe({
      next: response => {
        this.syncing = false;
        this.toast.success(`✔ ${response.message}`);

        setTimeout(() => {
          forkJoin({
            channels: this.guildService.getChannels(this.guildId),
            categories: this.guildService.getCategories(this.guildId),
            roles: this.guildService.getRoles(this.guildId)
          }).subscribe({
            next: ({ channels, categories, roles }) => {
              this.textChannels = channels.filter(isTextChannel);
              this.categories = categories.length > 0 ? categories : channels.filter(isCategoryChannel);
              this.roles = roles;
              this.assignableRoles = roles.filter(isAssignableRole);
            },
            error: () => {
              this.toast.error(this.translate.instant('settings.syncReloadFailed'));
            }
          });
        }, 5000);
      },
      error: err => {
        this.syncing = false;
        if (err.status === 401) {
          this.handleAuthError();
        } else {
          this.toast.error(getApiErrorMessage(err, this.translate.instant('errors.syncFailed')));
        }
      }
    });
  }

  save(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) {
      this.toast.error(this.translate.instant('settings.validation.fixBeforeSave'));
      return;
    }

    if (this.form.value.commandPanelEnabled && this.enabledPanelButtonCount === 0) {
      this.toast.error(this.translate.instant('settings.panel.validation.noButtons'));
      return;
    }

    this.saving = true;

    const value = this.form.value;
    const payload = {
      welcomeEnabled: value.welcomeEnabled,
      welcomeChannelId: value.welcomeChannelId || null,
      welcomeMessage: value.welcomeMessage?.trim(),
      autoRoleEnabled: value.autoRoleEnabled,
      autoRoleId: value.autoRoleId || null,
      logsEnabled: value.logsEnabled,
      logChannelId: value.logChannelId || null,
      ticketCategoryId: value.ticketCategoryId || null,
      ticketArchiveChannelId: value.ticketArchiveChannelId || null,
      ticketWelcomeTitle: value.ticketWelcomeTitle?.trim(),
      ticketWelcomeMessage: value.ticketWelcomeMessage?.trim(),
      ticketClosedMessage: value.ticketClosedMessage?.trim(),
      ticketClosedFromDashboardMessage: value.ticketClosedFromDashboardMessage?.trim(),
      ticketStaffReplyPrefix: value.ticketStaffReplyPrefix?.trim(),
      commandPanelEnabled: value.commandPanelEnabled,
      commandPanelChannelId: value.commandPanelChannelId || null,
      commandPanelTitle: value.commandPanelTitle?.trim(),
      commandPanelDescription: value.commandPanelDescription?.trim(),
      commandPanelImageUrl: value.commandPanelImageUrl?.trim() || null,
      commandPanelButtons: this.preparePanelButtonsForSave()
    };

    this.guildService.updateSettings(this.guildId, payload).subscribe({
      next: () => {
        this.saving = false;
        this.toast.success(this.translate.instant('settings.saved'));
      },
      error: err => {
        this.saving = false;
        if (err.status === 401) {
          this.handleAuthError();
        } else {
          this.toast.error(getApiErrorMessage(err, this.translate.instant('errors.saveSettings')));
        }
      }
    });
  }

  addPanelButton(): void {
    if (this.panelButtons.length >= 5) {
      return;
    }

    this.panelButtons = [
      ...this.panelButtons,
      {
        id: `btn-${Date.now()}`,
        action: 'ticket_open',
        label: 'New button',
        style: 'Secondary',
        enabled: true,
        order: this.panelButtons.length
      }
    ];
  }

  removePanelButton(index: number): void {
    this.panelButtons = this.panelButtons.filter((_, i) => i !== index);
  }

  movePanelButton(index: number, direction: -1 | 1): void {
    const target = index + direction;
    if (target < 0 || target >= this.panelButtons.length) {
      return;
    }

    const buttons = [...this.panelButtons];
    [buttons[index], buttons[target]] = [buttons[target], buttons[index]];
    this.panelButtons = buttons.map((button, order) => ({ ...button, order }));
  }

  get enabledPanelButtonCount(): number {
    return this.panelButtons.filter(button => button.enabled).length;
  }

  resetAutoReplyForm(): void {
    this.editingAutoReplyId = '';
    this.autoReplyForm = {
      trigger: '',
      response: '',
      matchMode: 'Contains',
      scope: 'AllChannels',
      enabled: true,
      priority: 0
    };
  }

  editAutoReply(rule: AutoReplyRule): void {
    this.activeTab = 'autoReplies';
    this.editingAutoReplyId = rule.id;
    this.autoReplyForm = {
      trigger: rule.trigger,
      response: rule.response,
      matchMode: (rule.matchMode as AutoReplyMatchMode) || 'Contains',
      scope: (rule.scope as AutoReplyScope) || 'AllChannels',
      enabled: rule.enabled,
      priority: rule.priority
    };
  }

  saveAutoReply(): void {
    const trigger = this.autoReplyForm.trigger.trim();
    const response = this.autoReplyForm.response.trim();
    if (!trigger || !response) {
      this.toast.error(this.translate.instant('settings.autoReplies.validation.required'));
      return;
    }

    this.autoReplySaving = true;
    const payload: CreateAutoReplyRule = {
      trigger,
      response,
      matchMode: this.autoReplyForm.matchMode,
      scope: this.autoReplyForm.scope,
      enabled: this.autoReplyForm.enabled,
      priority: this.autoReplyForm.priority
    };

    const request = this.editingAutoReplyId
      ? this.guildService.updateAutoReply(this.guildId, this.editingAutoReplyId, payload)
      : this.guildService.createAutoReply(this.guildId, payload);

    request.subscribe({
      next: () => {
        this.autoReplySaving = false;
        this.toast.success(this.translate.instant('settings.autoReplies.saved'));
        this.resetAutoReplyForm();
        this.loadAutoReplies();
      },
      error: err => {
        this.autoReplySaving = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('settings.autoReplies.saveError')));
      }
    });
  }

  deleteAutoReply(rule: AutoReplyRule): void {
    if (!window.confirm(this.translate.instant('settings.autoReplies.deleteConfirm', { trigger: rule.trigger }))) {
      return;
    }

    this.guildService.deleteAutoReply(this.guildId, rule.id).subscribe({
      next: () => {
        this.toast.success(this.translate.instant('settings.autoReplies.deleted'));
        if (this.editingAutoReplyId === rule.id) {
          this.resetAutoReplyForm();
        }
        this.loadAutoReplies();
      },
      error: err => {
        this.toast.error(getApiErrorMessage(err, this.translate.instant('settings.autoReplies.deleteError')));
      }
    });
  }

  selectTab(tabId: SettingsTabId): void {
    this.activeTab = tabId;
  }

  isTabActive(tabId: SettingsTabId): boolean {
    return this.activeTab === tabId;
  }

  isTabVisible(tab: SettingsTab): boolean {
    return !tab.requiresTickets || this.ticketsEnabled;
  }

  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    const enabledFeatures = [
      this.form?.get('welcomeEnabled')?.value,
      this.form?.get('autoRoleEnabled')?.value,
      this.form?.get('logsEnabled')?.value,
      this.form?.get('commandPanelEnabled')?.value
    ].filter(Boolean).length;
    const visibleTabs = this.settingsTabs.filter(tab => this.isTabVisible(tab)).length;

    return [
      {
        label: this.translate.instant('workspaceHero.settings.stats.features'),
        value: String(enabledFeatures)
      },
      {
        label: this.translate.instant('workspaceHero.settings.stats.sections'),
        value: String(visibleTabs)
      },
      {
        label: this.translate.instant('workspaceHero.settings.stats.channels'),
        value: String(this.textChannels.length)
      },
      {
        label: this.translate.instant('workspaceHero.settings.stats.autoReplies'),
        value: String(this.autoReplies.length)
      }
    ];
  }

  get workspaceHeroFooter(): string {
    return this.translate.instant('workspaceHero.settings.footer');
  }

  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction {
    return {
      label: this.translate.instant('workspaceHero.settings.cta.save'),
      type: 'submit',
      disabled: this.saving || this.form?.invalid,
      loading: this.saving,
      hidden: !this.showSaveButton
    };
  }

  get showSaveButton(): boolean {
    return this.activeTab !== 'general' && this.activeTab !== 'autoReplies' && !this.isWelcomeTab && !this.isAutoRoleTab;
  }

  get isWelcomeTab(): boolean {
    return this.activeTab === 'welcome';
  }

  get isAutoRoleTab(): boolean {
    return this.activeTab === 'autoRole';
  }

  get isWorkspaceTab(): boolean {
    return this.isWelcomeTab || this.isAutoRoleTab;
  }

  get heroIcon(): PageWorkspaceHeroIconName {
    if (this.isWelcomeTab) {
      return 'bell';
    }

    if (this.isAutoRoleTab) {
      return 'roles';
    }

    return 'settings';
  }

  get heroTitleKey(): string {
    if (this.isWelcomeTab) {
      return 'welcome.workspace.title';
    }

    if (this.isAutoRoleTab) {
      return 'autoRole.workspace.title';
    }

    return 'titles.settings';
  }

  get heroDescriptionKey(): string {
    if (this.isWelcomeTab) {
      return 'welcome.workspace.subtitle';
    }

    if (this.isAutoRoleTab) {
      return 'autoRole.workspace.subtitle';
    }

    return 'titles.settingsSubtitle';
  }

  get heroAriaLabelKey(): string {
    if (this.isWelcomeTab) {
      return 'welcome.workspace.hero.ariaLabel';
    }

    if (this.isAutoRoleTab) {
      return 'autoRole.workspace.hero.ariaLabel';
    }

    return 'workspaceHero.settings.ariaLabel';
  }

  get heroStats(): PageWorkspaceHeroStat[] {
    if (this.isWelcomeTab) {
      return this.welcomeWorkspaceHeroStats;
    }

    if (this.isAutoRoleTab) {
      return this.autoRoleWorkspaceHeroStats;
    }

    return this.workspaceHeroStats;
  }

  get heroFooterKey(): string {
    if (this.isWelcomeTab) {
      return this.welcomeWorkspaceHeroFooter;
    }

    if (this.isAutoRoleTab) {
      return this.autoRoleWorkspaceHeroFooter;
    }

    return this.workspaceHeroFooter;
  }

  get heroPrimaryAction(): PageWorkspaceHeroAction {
    if (this.isWelcomeTab) {
      return this.welcomeWorkspaceHeroPrimaryAction;
    }

    if (this.isAutoRoleTab) {
      return this.autoRoleWorkspaceHeroPrimaryAction;
    }

    return this.workspaceHeroPrimaryAction;
  }

  get autoRoleSelectedRoleLabel(): string {
    const roleId = this.form?.get('autoRoleId')?.value;
    if (!roleId) {
      return '';
    }

    const role = this.roles.find(item => item.discordRoleId === roleId);
    return role ? roleLabel(role) : String(roleId);
  }

  get autoRolePermissionStatus(): AutoRolePermissionStatus {
    if (!this.form?.get('autoRoleEnabled')?.value) {
      return 'unknown';
    }

    const role = this.autoRoleSelectedRole;
    if (!role) {
      return 'unknown';
    }

    if (role.isManaged) {
      return 'blockedManaged';
    }

    const botRole = this.autoRoleBotRole;
    if (!botRole) {
      return 'unknown';
    }

    if (role.position >= botRole.position) {
      return 'blockedHierarchy';
    }

    return 'ready';
  }

  get autoRoleLogsRoute(): string {
    return `/guilds/${this.guildId}/logs`;
  }

  get autoRoleWorkspaceHeroStats(): PageWorkspaceHeroStat[] {
    return [
      {
        label: this.translate.instant('autoRole.workspace.stats.enabled'),
        value: this.form?.get('autoRoleEnabled')?.value
          ? this.translate.instant('common.enabled')
          : this.translate.instant('common.disabled')
      },
      {
        label: this.translate.instant('autoRole.workspace.stats.role'),
        value: this.autoRoleSelectedRoleLabel || this.translate.instant('autoRole.workspace.stats.noRole')
      },
      {
        label: this.translate.instant('autoRole.workspace.stats.newMembers'),
        value: this.form?.get('autoRoleEnabled')?.value
          ? this.translate.instant('autoRole.workspace.stats.newMembersValue')
          : this.translate.instant('common.emptyValue')
      },
      {
        label: this.translate.instant('autoRole.workspace.stats.botPermission'),
        value: this.autoRolePermissionStatusLabel
      }
    ];
  }

  get autoRolePermissionStatusLabel(): string {
    switch (this.autoRolePermissionStatus) {
      case 'ready':
        return this.translate.instant('autoRole.workspace.stats.permissionReady');
      case 'blockedManaged':
        return this.translate.instant('autoRole.workspace.stats.permissionManaged');
      case 'blockedHierarchy':
        return this.translate.instant('autoRole.workspace.stats.permissionHierarchy');
      default:
        return this.translate.instant('autoRole.workspace.stats.permissionUnknown');
    }
  }

  get autoRoleWorkspaceHeroFooter(): string {
    if (!this.form?.get('autoRoleEnabled')?.value) {
      return this.translate.instant('autoRole.workspace.footer.disabled');
    }

    if (!this.form?.get('autoRoleId')?.value) {
      return this.translate.instant('autoRole.workspace.footer.noRole');
    }

    if (this.autoRolePermissionStatus === 'blockedManaged' || this.autoRolePermissionStatus === 'blockedHierarchy') {
      return this.translate.instant('autoRole.workspace.footer.blocked');
    }

    if (this.autoRolePermissionStatus === 'unknown') {
      return this.translate.instant('autoRole.workspace.footer.unknown');
    }

    return this.translate.instant('autoRole.workspace.footer.ready');
  }

  get autoRoleWorkspaceHeroPrimaryAction(): PageWorkspaceHeroAction {
    return {
      label: this.translate.instant('autoRole.workspace.cta.save'),
      type: 'submit',
      disabled: this.saving || this.form?.invalid,
      loading: this.saving
    };
  }

  private get autoRoleSelectedRole(): DiscordRole | null {
    const roleId = this.form?.get('autoRoleId')?.value;
    if (!roleId) {
      return null;
    }

    return this.roles.find(item => item.discordRoleId === roleId) ?? null;
  }

  private get autoRoleBotRole(): DiscordRole | null {
    const managedRoles = this.roles.filter(role => role.isManaged);
    if (managedRoles.length === 0) {
      return null;
    }

    return managedRoles.reduce((highest, role) => (role.position > highest.position ? role : highest));
  }

  get welcomeWorkspaceHeroStats(): PageWorkspaceHeroStat[] {
    return [
      {
        label: this.translate.instant('welcome.workspace.stats.enabled'),
        value: this.form?.get('welcomeEnabled')?.value
          ? this.translate.instant('common.enabled')
          : this.translate.instant('common.disabled')
      },
      {
        label: this.translate.instant('welcome.workspace.stats.previewReady'),
        value: this.welcomePreviewReady
          ? this.translate.instant('welcome.workspace.stats.readyValue')
          : this.translate.instant('welcome.workspace.stats.notReadyValue')
      },
      {
        label: this.translate.instant('welcome.workspace.stats.variablesUsed'),
        value: String(this.welcomeVariablesUsedCount)
      },
      {
        label: this.translate.instant('welcome.workspace.stats.testStatus'),
        value: this.translate.instant(
          this.welcomePreviewReady
            ? 'welcome.workspace.stats.testReady'
            : 'welcome.workspace.stats.testPending'
        )
      }
    ];
  }

  get welcomeWorkspaceHeroFooter(): string {
    if (!this.form?.get('welcomeEnabled')?.value) {
      return this.translate.instant('welcome.workspace.footer.disabled');
    }

    if (!this.welcomePreviewReady) {
      return this.translate.instant('welcome.workspace.footer.incomplete');
    }

    return this.translate.instant('welcome.workspace.footer.ready');
  }

  get welcomeWorkspaceHeroPrimaryAction(): PageWorkspaceHeroAction {
    return {
      label: this.translate.instant('welcome.workspace.cta.save'),
      type: 'submit',
      disabled: this.saving || this.form?.invalid,
      loading: this.saving
    };
  }

  get welcomePreviewReady(): boolean {
    return !!(
      this.form?.get('welcomeEnabled')?.value &&
      this.form?.get('welcomeChannelId')?.value &&
      this.form?.get('welcomeMessage')?.value?.trim()
    );
  }

  get welcomeVariablesUsedCount(): number {
    const message = String(this.form?.get('welcomeMessage')?.value ?? '');
    return this.welcomeVariables.filter(token => message.includes(token)).length;
  }

  get welcomeSelectedChannelLabel(): string {
    const channelId = this.form?.get('welcomeChannelId')?.value;
    if (!channelId) {
      return '';
    }

    const channel = this.textChannels.find(item => item.discordChannelId === channelId);
    return channel ? channelLabel(channel) : channelId;
  }

  get welcomePreviewMessage(): string {
    return this.formatWelcomePreview(String(this.form?.get('welcomeMessage')?.value ?? ''));
  }

  get welcomePreviewFooter(): string {
    const server = this.guildName || this.translate.instant('welcome.workspace.sample.server');
    return this.translate.instant('welcome.workspace.preview.footerTemplate', { server });
  }

  insertWelcomeVariable(variable: string): void {
    const control = this.form.get('welcomeMessage');
    if (!control) {
      return;
    }

    const active = document.activeElement;
    if (active instanceof HTMLTextAreaElement && active.classList.contains('welcome-message-input')) {
      const start = active.selectionStart ?? active.value.length;
      const end = active.selectionEnd ?? start;
      const current = String(control.value ?? '');
      const next = `${current.slice(0, start)}${variable}${current.slice(end)}`;
      control.setValue(next);
      control.markAsDirty();
      control.markAsTouched();

      requestAnimationFrame(() => {
        active.focus();
        const cursor = start + variable.length;
        active.setSelectionRange(cursor, cursor);
      });
      return;
    }

    control.setValue(`${String(control.value ?? '')}${variable}`);
    control.markAsDirty();
    control.markAsTouched();
  }

  fieldError(controlName: string): string | null {
    const control = this.form.get(controlName);
    if (!control || !control.touched || !control.errors) {
      return null;
    }

    if (control.errors['required'] || control.errors['requiredWhenEnabled']) {
      return this.translate.instant('settings.validation.requiredWhenEnabled');
    }
    if (control.errors['maxlength']) {
      return this.translate.instant('settings.validation.maxLength');
    }
    if (control.errors['snowflake'] || control.errors['invalid']) {
      return this.translate.instant('settings.validation.invalid');
    }

    return null;
  }

  private normalizePanelButtons(buttons?: CommandPanelButton[]): CommandPanelButton[] {
    if (!buttons?.length) {
      return DEFAULT_COMMAND_PANEL_BUTTONS.map(button => ({ ...button }));
    }

    return buttons.map((button, index) => ({
      ...button,
      order: button.order ?? index
    }));
  }

  private preparePanelButtonsForSave(): CommandPanelButton[] {
    return this.panelButtons.map((button, index) => ({
      ...button,
      label: button.label.trim(),
      order: index
    }));
  }

  private handleAuthError(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  private formatWelcomePreview(template: string): string {
    const server = this.guildName || this.translate.instant('welcome.workspace.sample.server');
    return template
      .replace(/\{user\}/gi, '@NewMember')
      .replace(/\{mention\}/gi, '@NewMember')
      .replace(/\{username\}/gi, 'NewMember')
      .replace(/\{server\}/gi, server)
      .replace(/\{memberCount\}/gi, '1,234');
  }
}

type SettingsTabId = 'general' | 'welcome' | 'autoRole' | 'logs' | 'tickets' | 'autoReplies' | 'panel';

interface SettingsTab {
  id: SettingsTabId;
  labelKey: string;
  requiresTickets?: boolean;
}
