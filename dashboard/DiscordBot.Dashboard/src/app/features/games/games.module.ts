import { NgModule } from '@angular/core';
import { SharedUiModule } from '../../shared/shared-ui.module';
import { GamesRoutingModule } from './games-routing.module';
import { GamesSettingsComponent } from './games-settings.component';
@NgModule({ declarations: [GamesSettingsComponent], imports: [SharedUiModule, GamesRoutingModule] })
export class GamesModule { }
