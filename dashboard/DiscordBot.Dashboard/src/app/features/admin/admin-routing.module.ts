import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminHomeComponent } from './admin-home/admin-home.component';
import { AdminGuildsComponent } from './admin-guilds/admin-guilds.component';
import { AdminUsersComponent } from './admin-users/admin-users.component';
import { AdminUpgradeRequestsComponent } from './admin-upgrade-requests/admin-upgrade-requests.component';
import { AdminPlansComponent } from './admin-plans/admin-plans.component';

const routes: Routes = [
  { path: '', component: AdminHomeComponent },
  { path: 'guilds', component: AdminGuildsComponent },
  { path: 'users', component: AdminUsersComponent },
  { path: 'upgrade-requests', component: AdminUpgradeRequestsComponent },
  { path: 'plans', component: AdminPlansComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
