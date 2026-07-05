import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { GamesSettingsComponent } from './games-settings.component';
@NgModule({ imports: [RouterModule.forChild([{ path: '', component: GamesSettingsComponent }])], exports: [RouterModule] })
export class GamesRoutingModule { }
