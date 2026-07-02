import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CreateGuildPermissionRoleRequest,
  GUILD_PERMISSION_OPTIONS,
  GuildPermissionKey,
  GuildPermissionRole
} from '../../core/models/staff.models';
import { DiscordRole, isAssignableRole } from '../../core/models/guild.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-staff',
  templateUrl: './staff.component.html',
  styleUrls: ['./staff.component.css']
})
export class StaffComponent implements OnInit {
  guildId = '';
  roles: GuildPermissionRole[] = [];
  discordRoles: DiscordRole[] = [];
  loading = true;
  error = '';
  saving = false;
  roleName = '';
  discordRoleId = '';
  selectedPermissions: Record<GuildPermissionKey, boolean> = this.emptyPermissions();
  readonly permissionOptions = GUILD_PERMISSION_OPTIONS;

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

    this.guildService.getStaff(this.guildId).subscribe({
      next: roles => {
        this.roles = roles;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('staff.loadError'));
        this.loading = false;
      }
    });

    this.guildService.getRoles(this.guildId).subscribe({
      next: roles => { this.discordRoles = roles.filter(isAssignableRole); },
      error: () => { this.discordRoles = []; }
    });
  }

  addRole(): void {
    const name = this.roleName.trim();
    const discordRoleId = this.discordRoleId.trim();
    const permissionKeys = this.selectedPermissionKeys();

    if (!name || !discordRoleId || permissionKeys.length === 0 || this.saving) {
      this.toast.error(this.translate.instant('staff.validation.required'));
      return;
    }

    this.saving = true;
    const request: CreateGuildPermissionRoleRequest = {
      name,
      discordRoleId,
      permissionKeys
    };

    this.guildService.addStaff(this.guildId, request).subscribe({
      next: role => {
        this.roles = [...this.roles, role];
        this.resetForm();
        this.saving = false;
        this.toast.success(this.translate.instant('staff.added'));
      },
      error: err => {
        this.saving = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('staff.addError')));
      }
    });
  }

  removeRole(role: GuildPermissionRole): void {
    this.guildService.removeStaff(this.guildId, role.id).subscribe({
      next: () => {
        this.roles = this.roles.filter(item => item.id !== role.id);
        this.toast.success(this.translate.instant('staff.removed'));
      },
      error: err => {
        this.toast.error(getApiErrorMessage(err, this.translate.instant('staff.removeError')));
      }
    });
  }

  formatPermissions(role: GuildPermissionRole): string {
    if (!role.permissionKeys?.length) {
      return '—';
    }

    return role.permissionKeys
      .map(key => {
        const option = this.permissionOptions.find(item => item.value === key);
        return option ? this.translate.instant(option.labelKey) : key;
      })
      .join(', ');
  }

  discordRoleLabel(role: GuildPermissionRole): string {
    return role.discordRoleName
      ? `@${role.discordRoleName}`
      : role.discordRoleId;
  }

  private selectedPermissionKeys(): GuildPermissionKey[] {
    return this.permissionOptions
      .map(option => option.value)
      .filter(key => this.selectedPermissions[key]);
  }

  private resetForm(): void {
    this.roleName = '';
    this.discordRoleId = '';
    this.selectedPermissions = this.emptyPermissions();
  }

  private emptyPermissions(): Record<GuildPermissionKey, boolean> {
    return {
      AccessModeration: false,
      AccessLogs: false,
      AccessTickets: false,
      ManagePermissionRoles: false
    };
  }
}
