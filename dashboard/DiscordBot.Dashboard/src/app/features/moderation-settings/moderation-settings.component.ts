import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CreateModerationPermissionRole,
  DiscordRole,
  MODERATION_PERMISSION_OPTIONS,
  ModerationPermissionKey,
  ModerationPermissionRole,
  roleLabel,
  isAssignableRole
} from '../../core/models/guild.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-moderation-settings',
  templateUrl: './moderation-settings.component.html',
  styleUrls: ['./moderation-settings.component.css']
})
export class ModerationSettingsComponent implements OnInit {
  guildId = '';
  roles: ModerationPermissionRole[] = [];
  discordRoles: DiscordRole[] = [];
  loading = true;
  error = '';
  saving = false;
  editingRoleId = '';
  roleDiscordId = '';
  permissions: Record<ModerationPermissionKey, boolean> = this.emptyPermissions();
  readonly permissionOptions = MODERATION_PERMISSION_OPTIONS;
  roleLabel = roleLabel;

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
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    this.guildService.getModerationPermissionRoles(this.guildId).subscribe({
      next: roles => {
        this.roles = roles;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('moderationSettings.loadError'));
        this.loading = false;
      }
    });

    this.guildService.getRoles(this.guildId).subscribe({
      next: roles => { this.discordRoles = roles.filter(isAssignableRole); },
      error: () => { this.discordRoles = []; }
    });
  }

  saveRole(): void {
    const roleDiscordId = this.roleDiscordId.trim();
    const permissionKeys = this.selectedPermissionKeys();

    if (!roleDiscordId || permissionKeys.length === 0 || this.saving) {
      this.toast.error(this.translate.instant('moderationSettings.validation.required'));
      return;
    }

    this.saving = true;
    const payload = this.buildPayload(roleDiscordId);

    const request = this.editingRoleId
      ? this.guildService.updateModerationPermissionRole(this.guildId, this.editingRoleId, payload)
      : this.guildService.createModerationPermissionRole(this.guildId, payload);

    request.subscribe({
      next: role => {
        if (this.editingRoleId) {
          this.roles = this.roles.map(item => (item.id === role.id ? role : item));
        } else {
          this.roles = [...this.roles, role];
        }
        this.resetForm();
        this.saving = false;
        this.toast.success(this.translate.instant('moderationSettings.saved'));
      },
      error: err => {
        this.saving = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('moderationSettings.saveError')));
      }
    });
  }

  editRole(role: ModerationPermissionRole): void {
    this.editingRoleId = role.id;
    this.roleDiscordId = role.roleDiscordId;
    this.permissions = {
      canWarn: role.canWarn,
      canViewWarnings: role.canViewWarnings,
      canClearMessages: role.canClearMessages,
      canKick: role.canKick,
      canViewModerationCases: role.canViewModerationCases,
      canViewLogs: role.canViewLogs
    };
  }

  deleteRole(role: ModerationPermissionRole): void {
    if (!window.confirm(this.translate.instant('moderationSettings.deleteConfirm', { role: this.roleName(role) }))) {
      return;
    }

    this.guildService.deleteModerationPermissionRole(this.guildId, role.id).subscribe({
      next: () => {
        this.roles = this.roles.filter(item => item.id !== role.id);
        if (this.editingRoleId === role.id) {
          this.resetForm();
        }
        this.toast.success(this.translate.instant('moderationSettings.deleted'));
      },
      error: err => {
        this.toast.error(getApiErrorMessage(err, this.translate.instant('moderationSettings.deleteError')));
      }
    });
  }

  cancelEdit(): void {
    this.resetForm();
  }

  formatPermissions(role: ModerationPermissionRole): string {
    const labels = this.permissionOptions
      .filter(option => role[option.value])
      .map(option => this.translate.instant(option.labelKey));

    return labels.length > 0 ? labels.join(', ') : '—';
  }

  roleName(role: ModerationPermissionRole): string {
    return role.roleName ? `@${role.roleName}` : role.roleDiscordId;
  }

  get formTitleKey(): string {
    return this.editingRoleId ? 'moderationSettings.editTitle' : 'moderationSettings.addTitle';
  }

  private buildPayload(roleDiscordId: string): CreateModerationPermissionRole {
    return {
      roleDiscordId,
      canWarn: this.permissions.canWarn,
      canViewWarnings: this.permissions.canViewWarnings,
      canClearMessages: this.permissions.canClearMessages,
      canKick: this.permissions.canKick,
      canViewModerationCases: this.permissions.canViewModerationCases,
      canViewLogs: this.permissions.canViewLogs
    };
  }

  private selectedPermissionKeys(): ModerationPermissionKey[] {
    return this.permissionOptions
      .map(option => option.value)
      .filter(key => this.permissions[key]);
  }

  private resetForm(): void {
    this.editingRoleId = '';
    this.roleDiscordId = '';
    this.permissions = this.emptyPermissions();
  }

  private emptyPermissions(): Record<ModerationPermissionKey, boolean> {
    return {
      canWarn: false,
      canViewWarnings: false,
      canClearMessages: false,
      canKick: false,
      canViewModerationCases: false,
      canViewLogs: false
    };
  }
}
