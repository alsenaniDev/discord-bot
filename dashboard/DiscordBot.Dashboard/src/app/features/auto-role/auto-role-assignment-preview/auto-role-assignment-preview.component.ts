import { Component, Input } from '@angular/core';
import { AutoRolePermissionStatus } from '../auto-role-editor/auto-role-editor.component';

export type AutoRolePreviewState = 'inactive' | 'empty' | 'blocked' | 'ready';

@Component({
  selector: 'app-auto-role-assignment-preview',
  templateUrl: './auto-role-assignment-preview.component.html',
  styleUrls: ['./auto-role-assignment-preview.component.css']
})
export class AutoRoleAssignmentPreviewComponent {
  @Input() enabled = false;
  @Input() roleName = '';
  @Input() guildName = '';
  @Input() permissionStatus: AutoRolePermissionStatus = 'unknown';

  get previewState(): AutoRolePreviewState {
    if (!this.enabled) {
      return 'inactive';
    }

    if (!this.roleName) {
      return 'empty';
    }

    if (this.permissionStatus === 'blockedManaged' || this.permissionStatus === 'blockedHierarchy') {
      return 'blocked';
    }

    return 'ready';
  }
}
