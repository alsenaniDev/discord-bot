import { NgModule } from '@angular/core'; import { RouterModule } from '@angular/router'; import { MusicSettingsComponent } from './music-settings.component';
@NgModule({ imports: [RouterModule.forChild([{ path: '', component: MusicSettingsComponent }])], exports: [RouterModule] }) export class MusicRoutingModule { }
