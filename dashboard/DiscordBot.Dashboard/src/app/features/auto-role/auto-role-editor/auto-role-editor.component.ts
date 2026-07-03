import { Component, Input } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { DiscordRole, roleLabel } from '../../../core/models/guild.models';

export type AutoRolePermissionStatus = 'ready' | 'blockedManaged' | 'blockedHierarchy' | 'unknown';

@Component({
  selector: 'app-auto-role-editor',
  templateUrl: './auto-role-editor.component.html',
  styleUrls: ['./auto-role-editor.component.css']
})
export class AutoRoleEditorComponent {
  @Input() form!: FormGroup;
  @Input() assignableRoles: DiscordRole[] = [];
  @Input() permissionStatus: AutoRolePermissionStatus = 'unknown';
  @Input() fieldErrorFn: (controlName: string) => string | null = () => null;

  roleLabel = roleLabel;

  fieldError(controlName: string): string | null {
    return this.fieldErrorFn(controlName);
  }
}
