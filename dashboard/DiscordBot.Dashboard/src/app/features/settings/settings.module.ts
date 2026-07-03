import { NgModule } from '@angular/core';
import { SettingsComponent } from './settings.component';
import { SettingsRoutingModule } from './settings-routing.module';
import { WelcomeEditorComponent } from '../welcome/welcome-editor/welcome-editor.component';
import { WelcomeDiscordPreviewComponent } from '../welcome/welcome-discord-preview/welcome-discord-preview.component';
import { WelcomeTestSectionComponent } from '../welcome/welcome-test-section/welcome-test-section.component';
import { AutoRoleEditorComponent } from '../auto-role/auto-role-editor/auto-role-editor.component';
import { AutoRoleAssignmentPreviewComponent } from '../auto-role/auto-role-assignment-preview/auto-role-assignment-preview.component';
import { AutoRoleNotesComponent } from '../auto-role/auto-role-notes/auto-role-notes.component';
import { SharedUiModule } from '../../shared/shared-ui.module';

@NgModule({
  declarations: [
    SettingsComponent,
    WelcomeEditorComponent,
    WelcomeDiscordPreviewComponent,
    WelcomeTestSectionComponent,
    AutoRoleEditorComponent,
    AutoRoleAssignmentPreviewComponent,
    AutoRoleNotesComponent
  ],
  imports: [
    SharedUiModule,
    SettingsRoutingModule
  ]
})
export class SettingsModule {}
