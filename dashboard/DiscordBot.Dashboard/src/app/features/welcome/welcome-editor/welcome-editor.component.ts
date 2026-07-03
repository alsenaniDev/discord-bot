import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { DiscordChannel, channelLabel } from '../../../core/models/guild.models';

export type WelcomeEditorField = 'welcomeMessage';

@Component({
  selector: 'app-welcome-editor',
  templateUrl: './welcome-editor.component.html',
  styleUrls: ['./welcome-editor.component.css']
})
export class WelcomeEditorComponent {
  @Input() form!: FormGroup;
  @Input() textChannels: DiscordChannel[] = [];
  @Input() guildName = '';
  @Input() variables: string[] = [];
  @Input() variablesUsedCount = 0;
  @Input() testChannelLabel = '';
  @Input() testEnabled = false;
  @Input() testPreviewReady = false;
  @Input() fieldErrorFn: (controlName: string) => string | null = () => null;

  @Output() fieldFocus = new EventEmitter<WelcomeEditorField>();
  @Output() variableInsert = new EventEmitter<string>();

  channelLabel = channelLabel;

  fieldError(controlName: string): string | null {
    return this.fieldErrorFn(controlName);
  }

  onFieldFocus(field: WelcomeEditorField): void {
    this.fieldFocus.emit(field);
  }

  onVariableInsert(variable: string): void {
    this.variableInsert.emit(variable);
  }
}
