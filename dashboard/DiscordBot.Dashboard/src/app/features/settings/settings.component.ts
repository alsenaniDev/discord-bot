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
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import { requiredWhenEnabled } from '../../core/utils/settings.validators';

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
      ticketCategoryId: ['']
    });

    this.form.get('welcomeEnabled')?.valueChanges.subscribe(() => this.form.get('welcomeChannelId')?.updateValueAndValidity());
    this.form.get('autoRoleEnabled')?.valueChanges.subscribe(() => this.form.get('autoRoleId')?.updateValueAndValidity());
    this.form.get('logsEnabled')?.valueChanges.subscribe(() => this.form.get('logChannelId')?.updateValueAndValidity());

    this.loadPageData();
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
        this.textChannels = channels.filter(isTextChannel);
        this.categories = categories.length > 0 ? categories : channels.filter(isCategoryChannel);
        this.roles = roles;
        this.assignableRoles = roles.filter(isAssignableRole);

        this.form.patchValue({
          welcomeEnabled: settings.welcomeEnabled,
          welcomeChannelId: settings.welcomeChannelId ?? '',
          welcomeMessage: settings.welcomeMessage,
          autoRoleEnabled: settings.autoRoleEnabled,
          autoRoleId: settings.autoRoleId ?? '',
          logsEnabled: settings.logsEnabled,
          logChannelId: settings.logChannelId ?? '',
          ticketCategoryId: settings.ticketCategoryId ?? ''
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
      ticketCategoryId: value.ticketCategoryId || null
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

    return this.translate.instant('settings.validation.invalid');
  }

  private handleAuthError(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
