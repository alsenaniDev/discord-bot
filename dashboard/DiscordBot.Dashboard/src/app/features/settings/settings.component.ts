import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
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

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit {
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
    { id: 'autoReplies', labelKey: 'settings.tabs.autoReplies' },
    { id: 'panel', labelKey: 'settings.tabs.panel' }
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

  get showSaveButton(): boolean {
    return this.activeTab !== 'general' && this.activeTab !== 'autoReplies';
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
}

type SettingsTabId = 'general' | 'welcome' | 'autoRole' | 'logs' | 'tickets' | 'autoReplies' | 'panel';

interface SettingsTab {
  id: SettingsTabId;
  labelKey: string;
  requiresTickets?: boolean;
}
