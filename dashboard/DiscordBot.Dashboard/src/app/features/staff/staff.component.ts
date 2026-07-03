import { Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CreateGuildPermissionRoleRequest,
  GUILD_PERMISSION_OPTIONS,
  GuildPermissionKey,
  GuildPermissionRole,
  hasModerationBotPermissions,
  normalizePermissionKeys
} from '../../core/models/staff.models';
import { DiscordRole, isAssignableRole } from '../../core/models/guild.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroStat
} from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';
import { StatusBadgeTone } from '../../shared/ui/status-badge/status-badge.component';
import {
  StaffFilterBarComponent,
  StaffRoleFilter,
  StaffStatusFilter
} from './staff-filter-bar/staff-filter-bar.component';
import {
  StaffDetailPanelComponent,
  StaffPermissionGroupView
} from './staff-detail-panel/staff-detail-panel.component';
import { StaffRoleEditorComponent } from './staff-role-editor/staff-role-editor.component';
import { STAFF_PERMISSION_GROUPS, StaffPermissionGroupId } from './staff-permission-groups';

type StaffAccessTier = 'owner' | 'moderator' | 'support';

@Component({
  selector: 'app-staff',
  templateUrl: './staff.component.html',
  styleUrls: ['./staff.component.css']
})
export class StaffComponent implements OnInit {
  @ViewChild('staffDetailPanel') staffDetailPanel?: StaffDetailPanelComponent;
  @ViewChild('staffRoleEditor') staffRoleEditor?: StaffRoleEditorComponent;

  guildId = '';
  roles: GuildPermissionRole[] = [];
  discordRoles: DiscordRole[] = [];
  loading = true;
  error = '';
  saving = false;
  editorExpanded = false;
  editingRoleId = '';
  roleName = '';
  discordRoleId = '';
  selectedPermissions: Record<GuildPermissionKey, boolean> = this.emptyPermissions();
  selectedRoleId = '';
  uiSearch = '';
  uiRoleFilter: StaffRoleFilter = 'all';
  uiStatusFilter: StaffStatusFilter = 'all';
  uiPermissionFilter: StaffPermissionGroupId | 'all' = 'all';
  detailInline = true;
  mobileDetailOpen = false;

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
    this.updateDetailLayout();
    this.loadData();
  }

  @HostListener('window:resize')
  onResize(): void {
    this.updateDetailLayout();
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.editorMode && !this.saving) {
      this.cancelEdit();
    }
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    this.guildService.getStaff(this.guildId).subscribe({
      next: roles => {
        this.roles = roles;
        if (this.selectedRoleId && !roles.some(role => role.id === this.selectedRoleId)) {
          this.closeDetail();
        }
        if (this.editingRoleId && !roles.some(role => role.id === this.editingRoleId)) {
          this.resetEditor();
        }
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('staff.loadError'));
        this.loading = false;
      }
    });

    this.guildService.getRoles(this.guildId).subscribe({
      next: roles => {
        this.discordRoles = roles.filter(isAssignableRole);
      },
      error: () => {
        this.discordRoles = [];
      }
    });
  }

  get filteredRoles(): GuildPermissionRole[] {
    return this.roles.filter(role => this.matchesFilters(role));
  }

  get selectedRole(): GuildPermissionRole | null {
    return this.roles.find(role => role.id === this.selectedRoleId) ?? null;
  }

  get editingRole(): boolean {
    return !!this.editingRoleId;
  }

  get editorMode(): boolean {
    return this.editorExpanded;
  }

  get editingRoleCreatedAt(): string {
    const role = this.roles.find(item => item.id === this.editingRoleId);
    return role?.createdAt ?? '';
  }

  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    return [
      {
        label: this.translate.instant('staff.workspace.stats.roles'),
        value: String(this.roles.length)
      },
      {
        label: this.translate.instant('staff.workspace.stats.linked'),
        value: String(this.linkedDiscordRoleCount)
      },
      {
        label: this.translate.instant('staff.workspace.stats.groups'),
        value: String(this.activePermissionGroupCount)
      },
      {
        label: this.translate.instant('staff.workspace.stats.owners'),
        value: String(this.countRolesByTier('owner'))
      }
    ];
  }

  get workspaceHeroFooter(): string {
    return this.translate.instant('staff.workspace.footer');
  }

  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction {
    return {
      label: this.translate.instant('staff.workspace.cta.addRole')
    };
  }

  get selectedPermissionGroups(): StaffPermissionGroupView[] {
    if (!this.selectedRole) {
      return [];
    }

    const granted = new Set(normalizePermissionKeys(this.selectedRole.permissionKeys));

    return STAFF_PERMISSION_GROUPS.map(group => ({
      id: group.id,
      labelKey: group.labelKey,
      items: group.keys
        .filter(key => granted.has(key))
        .map(key => {
          const option = this.permissionOptions.find(item => item.value === key);
          return {
            labelKey: option?.labelKey ?? key,
            granted: true
          };
        })
    })).filter(group => group.items.length > 0);
  }

  onHeroPrimaryAction(): void {
    this.openEditor(false);
  }

  onCreateFirstRole(): void {
    this.openEditor(false);
  }

  openEditor(editExisting: boolean): void {
    this.mobileDetailOpen = false;
    this.editorExpanded = true;
    if (!editExisting && !this.editingRoleId) {
      this.resetFormFields();
    }
    this.focusEditor();
  }

  collapseEditor(): void {
    this.resetEditor();
  }

  selectRole(role: GuildPermissionRole): void {
    this.selectedRoleId = role.id;
    if (!this.detailInline) {
      this.mobileDetailOpen = true;
    }
    this.staffDetailPanel?.focusTitle();
  }

  closeDetail(): void {
    this.selectedRoleId = '';
    this.mobileDetailOpen = false;
  }

  clearFilters(): void {
    this.uiSearch = '';
    this.uiRoleFilter = 'all';
    this.uiStatusFilter = 'all';
    this.uiPermissionFilter = 'all';
  }

  discordRoleLabel(role: GuildPermissionRole): string {
    return role.discordRoleName
      ? `@${role.discordRoleName}`
      : role.discordRoleId;
  }

  discordRoleColor(role: GuildPermissionRole): string {
    const discordRole = this.discordRoles.find(item => item.discordRoleId === role.discordRoleId);
    return this.colorToHex(discordRole?.color);
  }

  roleStatusLabel(role: GuildPermissionRole): string {
    return this.translate.instant(
      this.isRoleActive(role) ? 'staff.workspace.status.active' : 'staff.workspace.status.inactive'
    );
  }

  roleStatusTone(role: GuildPermissionRole): StatusBadgeTone {
    return this.isRoleActive(role) ? 'success' : 'neutral';
  }

  roleUpdatedLabel(role: GuildPermissionRole): string {
    return this.translate.instant('staff.workspace.updated', {
      date: new Date(role.createdAt).toLocaleDateString()
    });
  }

  permissionCount(role: GuildPermissionRole): number {
    return normalizePermissionKeys(role.permissionKeys).length;
  }

  permissionGroupSummary(role: GuildPermissionRole): string {
    const granted = normalizePermissionKeys(role.permissionKeys);
    const groups = STAFF_PERMISSION_GROUPS
      .filter(group => group.keys.some(key => granted.includes(key)))
      .map(group => this.translate.instant(group.labelKey));

    return groups.length > 0
      ? groups.join(' · ')
      : this.translate.instant('staff.workspace.noPermissions');
  }

  isRoleEditing(role: GuildPermissionRole): boolean {
    return this.editingRoleId === role.id;
  }

  onEditPermissions(): void {
    if (!this.selectedRole) {
      return;
    }

    this.startEdit(this.selectedRole);
  }

  onScrollToEditor(): void {
    this.openEditor(!!this.editingRoleId);
  }

  onPermissionToggle(event: { key: GuildPermissionKey; value: boolean }): void {
    this.selectedPermissions = {
      ...this.selectedPermissions,
      [event.key]: event.value
    };
  }

  saveRole(): void {
    const name = this.roleName.trim();
    const discordRoleId = this.discordRoleId.trim();
    const permissionKeys = this.selectedPermissionKeys();

    if (!name || !discordRoleId || permissionKeys.length === 0 || this.saving) {
      this.toast.error(this.translate.instant('staff.validation.required'));
      return;
    }

    this.saving = true;
    const payload: CreateGuildPermissionRoleRequest = {
      name,
      discordRoleId,
      permissionKeys
    };

    const request = this.editingRoleId
      ? this.guildService.updatePermissionRole(this.guildId, this.editingRoleId, payload)
      : this.guildService.addStaff(this.guildId, payload);

    const wasEditing = !!this.editingRoleId;

    request.subscribe({
      next: role => {
        if (wasEditing) {
          this.roles = this.roles.map(item => (item.id === role.id ? role : item));
        } else {
          this.roles = [...this.roles, role];
        }

        this.resetEditor();
        this.saving = false;
        this.selectRole(role);
        this.toast.success(this.translate.instant(wasEditing ? 'staff.updated' : 'staff.added'));
      },
      error: err => {
        this.saving = false;
        this.toast.error(
          getApiErrorMessage(
            err,
            this.translate.instant(wasEditing ? 'staff.updateError' : 'staff.addError')
          )
        );
      }
    });
  }

  cancelEdit(): void {
    this.resetEditor();
  }

  removeRole(role: GuildPermissionRole): void {
    this.guildService.removeStaff(this.guildId, role.id).subscribe({
      next: () => {
        this.roles = this.roles.filter(item => item.id !== role.id);
        if (this.selectedRoleId === role.id) {
          this.closeDetail();
        }
        if (this.editingRoleId === role.id) {
          this.resetEditor();
        }
        this.toast.success(this.translate.instant('staff.removed'));
      },
      error: err => {
        this.toast.error(getApiErrorMessage(err, this.translate.instant('staff.removeError')));
      }
    });
  }

  private startEdit(role: GuildPermissionRole): void {
    this.editingRoleId = role.id;
    this.roleName = role.name;
    this.discordRoleId = role.discordRoleId;
    this.selectedPermissions = this.permissionsFromRole(role);
    this.mobileDetailOpen = false;
    this.editorExpanded = true;
    this.focusEditor();
  }

  private resetEditor(): void {
    this.editorExpanded = false;
    this.editingRoleId = '';
    this.resetFormFields();
  }

  private resetFormFields(): void {
    this.roleName = '';
    this.discordRoleId = '';
    this.selectedPermissions = this.emptyPermissions();
  }

  private focusEditor(): void {
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    requestAnimationFrame(() => {
      window.scrollTo({
        top: 0,
        left: 0,
        behavior: reducedMotion ? 'auto' : 'smooth'
      });

      document.getElementById('staff-editor-mode')?.scrollIntoView({
        block: 'start',
        behavior: reducedMotion ? 'auto' : 'smooth'
      });

      this.staffRoleEditor?.focusTitle();
    });
  }

  private permissionsFromRole(role: GuildPermissionRole): Record<GuildPermissionKey, boolean> {
    const granted = new Set(normalizePermissionKeys(role.permissionKeys));
    return GUILD_PERMISSION_OPTIONS.reduce(
      (acc, option) => {
        acc[option.value] = granted.has(option.value);
        return acc;
      },
      {} as Record<GuildPermissionKey, boolean>
    );
  }

  private get linkedDiscordRoleCount(): number {
    return new Set(this.roles.map(role => role.discordRoleId)).size;
  }

  private get activePermissionGroupCount(): number {
    const activeGroups = new Set<StaffPermissionGroupId>();

    for (const role of this.roles) {
      const granted = normalizePermissionKeys(role.permissionKeys);
      for (const group of STAFF_PERMISSION_GROUPS) {
        if (group.keys.some(key => granted.includes(key))) {
          activeGroups.add(group.id);
        }
      }
    }

    return activeGroups.size;
  }

  private countRolesByTier(tier: StaffAccessTier): number {
    return this.roles.filter(role => this.resolveRoleTier(role) === tier).length;
  }

  private resolveRoleTier(role: GuildPermissionRole): StaffAccessTier {
    const keys = normalizePermissionKeys(role.permissionKeys);
    if (keys.includes('ManagePermissionRoles') || keys.includes('ManageSettings')) {
      return 'owner';
    }

    if (hasModerationBotPermissions(keys)) {
      return 'moderator';
    }

    return 'support';
  }

  private isRoleActive(role: GuildPermissionRole): boolean {
    return normalizePermissionKeys(role.permissionKeys).length > 0;
  }

  private colorToHex(color?: number | null): string {
    if (color == null || color === 0) {
      return '#99aab5';
    }

    return `#${color.toString(16).padStart(6, '0')}`;
  }

  private matchesFilters(role: GuildPermissionRole): boolean {
    const query = this.uiSearch.trim().toLowerCase();
    if (query) {
      const haystack = `${role.name} ${role.discordRoleName ?? ''} ${role.discordRoleId}`.toLowerCase();
      if (!haystack.includes(query)) {
        return false;
      }
    }

    if (this.uiRoleFilter !== 'all' && this.resolveRoleTier(role) !== this.uiRoleFilter) {
      return false;
    }

    if (this.uiStatusFilter === 'active' && !this.isRoleActive(role)) {
      return false;
    }

    if (this.uiStatusFilter === 'inactive' && this.isRoleActive(role)) {
      return false;
    }

    if (this.uiPermissionFilter !== 'all') {
      const group = STAFF_PERMISSION_GROUPS.find(item => item.id === this.uiPermissionFilter);
      if (!group) {
        return true;
      }

      const granted = normalizePermissionKeys(role.permissionKeys);
      if (!group.keys.some(key => granted.includes(key))) {
        return false;
      }
    }

    return true;
  }

  private selectedPermissionKeys(): GuildPermissionKey[] {
    return this.permissionOptions
      .map(option => option.value)
      .filter(key => this.selectedPermissions[key]);
  }

  private emptyPermissions(): Record<GuildPermissionKey, boolean> {
    return GUILD_PERMISSION_OPTIONS.reduce(
      (acc, option) => {
        acc[option.value] = false;
        return acc;
      },
      {} as Record<GuildPermissionKey, boolean>
    );
  }

  private updateDetailLayout(): void {
    this.detailInline = window.matchMedia('(min-width: 960px)').matches;
    if (this.detailInline) {
      this.mobileDetailOpen = false;
    }
  }
}
