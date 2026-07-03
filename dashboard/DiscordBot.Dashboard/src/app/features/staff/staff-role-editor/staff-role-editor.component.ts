import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { DiscordRole } from '../../../core/models/guild.models';
import { GUILD_PERMISSION_OPTIONS, GuildPermissionKey } from '../../../core/models/staff.models';
import {
  STAFF_PERMISSION_GROUPS,
  StaffPermissionGroupDefinition,
  StaffPermissionGroupId
} from '../staff-permission-groups';

export interface StaffEditorPreviewGroup {
  id: string;
  labelKey: string;
  count: number;
  items: Array<{ labelKey: string }>;
}

@Component({
  selector: 'app-staff-role-editor',
  templateUrl: './staff-role-editor.component.html',
  styleUrls: ['./staff-role-editor.component.css']
})
export class StaffRoleEditorComponent {
  @Input() editing = false;
  @Input() roleName = '';
  @Input() discordRoleId = '';
  @Input() selectedPermissions: Record<GuildPermissionKey, boolean> = {} as Record<GuildPermissionKey, boolean>;
  @Input() discordRoles: DiscordRole[] = [];
  @Input() saving = false;
  @Input() disabled = false;
  @Input() roleId = '';
  @Input() createdAt = '';

  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  @Output() roleNameChange = new EventEmitter<string>();
  @Output() discordRoleIdChange = new EventEmitter<string>();
  @Output() permissionToggle = new EventEmitter<{ key: GuildPermissionKey; value: boolean }>();

  readonly permissionGroups = STAFF_PERMISSION_GROUPS;
  advancedExpanded = false;
  private openGroupIds = new Set<StaffPermissionGroupId>(['moderation']);

  constructor(private translate: TranslateService) {}

  get selectedPermissionCount(): number {
    return this.permissionGroups
      .flatMap(group => group.keys)
      .filter(key => this.selectedPermissions[key]).length;
  }

  get activeGroupCount(): number {
    return this.permissionGroups.filter(group => this.groupSelectedCount(group) > 0).length;
  }

  get previewGroups(): StaffEditorPreviewGroup[] {
    return this.permissionGroups
      .map(group => ({
        id: group.id,
        labelKey: group.labelKey,
        count: this.groupSelectedCount(group),
        items: group.keys
          .filter(key => this.selectedPermissions[key])
          .map(key => ({
            labelKey: this.permissionLabel(key)
          }))
      }))
      .filter(group => group.count > 0);
  }

  get selectedDiscordRoleName(): string {
    if (!this.discordRoleId) {
      return this.translate.instant('staff.editor.preview.noDiscordRole');
    }

    const role = this.discordRoles.find(item => item.discordRoleId === this.discordRoleId);
    return role?.name ?? this.discordRoleId;
  }

  permissionLabel(key: GuildPermissionKey): string {
    const option = GUILD_PERMISSION_OPTIONS.find(item => item.value === key);
    return option?.labelKey ?? key;
  }

  groupSelectedCount(group: StaffPermissionGroupDefinition): number {
    return group.keys.filter(key => this.selectedPermissions[key]).length;
  }

  isGroupOpen(group: StaffPermissionGroupDefinition): boolean {
    return this.openGroupIds.has(group.id);
  }

  onGroupToggle(group: StaffPermissionGroupDefinition, event: Event): void {
    const details = event.target as HTMLDetailsElement;

    if (details.open) {
      this.openGroupIds.add(group.id);
      return;
    }

    this.openGroupIds.delete(group.id);
  }

  onPermissionChange(key: GuildPermissionKey, checked: boolean): void {
    this.permissionToggle.emit({ key, value: checked });
  }

  onAdvancedToggle(event: Event): void {
    this.advancedExpanded = (event.target as HTMLDetailsElement).open;
  }

  onBack(): void {
    this.cancel.emit();
  }

  focusTitle(): void {
    requestAnimationFrame(() => {
      const title = document.getElementById('staff-editor-mode-title');
      if (!title) {
        return;
      }

      title.setAttribute('tabindex', '-1');
      title.focus({ preventScroll: true });
    });
  }
}
