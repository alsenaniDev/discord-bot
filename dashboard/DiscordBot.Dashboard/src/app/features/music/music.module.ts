import { NgModule } from '@angular/core'; import { SharedUiModule } from '../../shared/shared-ui.module'; import { MusicSettingsComponent } from './music-settings.component'; import { MusicRoutingModule } from './music-routing.module';
@NgModule({ declarations: [MusicSettingsComponent], imports: [SharedUiModule, MusicRoutingModule] }) export class MusicModule { }
