import { Component, EventEmitter, Input, Output } from '@angular/core';
import { GuildModule } from '../../../core/models/module.models';
import { ModuleIconName, ModuleIconTone } from '../config/module-workspace.config';

export type ModuleCardStatus = 'enabled' | 'disabled' | 'locked' | 'needsSetup';
export type ModuleCardAction = 'configure' | 'enable' | 'upgrade';

@Component({
  selector: 'app-modules-module-card',
  templateUrl: './modules-module-card.component.html',
  styleUrls: ['./modules-module-card.component.css']
})
export class ModulesModuleCardComponent {
  @Input() module!: GuildModule;
  @Input() icon: ModuleIconName = 'overview';
  @Input() iconTone: ModuleIconTone = 'blue';
  @Input() displayName = '';
  @Input() displayDescription = '';
  @Input() status: ModuleCardStatus = 'disabled';
  @Input() primaryAction: ModuleCardAction = 'enable';
  @Input() saving = false;
  @Input() canToggle = false;
  @Input() toggleChecked = false;

  @Output() primaryActionClick = new EventEmitter<void>();
  @Output() toggleChange = new EventEmitter<boolean>();

  onPrimaryAction(): void {
    this.primaryActionClick.emit();
  }

  onToggleClick(): void {
    if (!this.canToggle || this.saving) {
      return;
    }

    this.toggleChange.emit(!this.toggleChecked);
  }

  get statusLabelKey(): string {
    return `modules.status.${this.status}`;
  }

  get statusBadgeTone(): string {
    switch (this.status) {
      case 'enabled':
      case 'needsSetup':
        return 'success';
      case 'locked':
        return 'neutral';
      default:
        return 'warning';
    }
  }

  get primaryActionLabelKey(): string {
    return `modules.actions.${this.primaryAction}`;
  }

  get metaLabelKey(): string {
    if (this.status === 'locked') {
      return 'modules.card.lockedMeta';
    }

    if (this.status === 'enabled') {
      return 'modules.card.enabledMeta';
    }

    return 'modules.card.disabledMeta';
  }
}
