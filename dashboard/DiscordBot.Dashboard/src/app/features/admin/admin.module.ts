import { NgModule } from '@angular/core';
import { AdminHomeComponent } from './admin-home/admin-home.component';
import { AdminGuildsComponent } from './admin-guilds/admin-guilds.component';
import { AdminUsersComponent } from './admin-users/admin-users.component';
import { AdminUpgradeRequestsComponent } from './admin-upgrade-requests/admin-upgrade-requests.component';
import { AdminPlansComponent } from './admin-plans/admin-plans.component';
import { AdminGamesComponent } from './admin-games/admin-games.component';
import { AdminRoutingModule } from './admin-routing.module';
import { SharedUiModule } from '../../shared/shared-ui.module';

@NgModule({
  declarations: [
    AdminHomeComponent,
    AdminGuildsComponent,
    AdminUsersComponent,
    AdminUpgradeRequestsComponent,
    AdminPlansComponent,
    AdminGamesComponent
  ],
  imports: [
    SharedUiModule,
    AdminRoutingModule
  ]
})
export class AdminModule {}
