import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { GuildAccessGuard } from './core/guards/guild-access.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { CallbackComponent } from './features/auth/callback/callback.component';
import { DashboardLayoutComponent } from './features/layout/dashboard-layout.component';
import { ServersComponent } from './features/servers/servers.component';
import { OverviewComponent } from './features/overview/overview.component';
import { ModerationComponent } from './features/moderation/moderation.component';
import { SettingsComponent } from './features/settings/settings.component';
import { TicketsComponent } from './features/tickets/tickets.component';
import { ModulesComponent } from './features/modules/modules.component';
import { LogsComponent } from './features/logs/logs.component';
import { ReactionRolesComponent } from './features/reaction-roles/reaction-roles.component';
import { SubscriptionComponent } from './features/subscription/subscription.component';
import { StaffComponent } from './features/staff/staff.component';
import { AdminGuard } from './core/guards/admin.guard';
import { AdminHomeComponent } from './features/admin/admin-home/admin-home.component';
import { AdminGuildsComponent } from './features/admin/admin-guilds/admin-guilds.component';
import { AdminUsersComponent } from './features/admin/admin-users/admin-users.component';
import { AdminUpgradeRequestsComponent } from './features/admin/admin-upgrade-requests/admin-upgrade-requests.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'auth/callback', component: CallbackComponent },
  {
    path: '',
    component: DashboardLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'servers', component: ServersComponent },
      {
        path: 'guilds/:id/overview',
        component: OverviewComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/settings',
        component: SettingsComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/tickets',
        component: TicketsComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'moderation' }
      },
      {
        path: 'guilds/:id/moderation',
        component: ModerationComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'moderation' }
      },
      {
        path: 'guilds/:id/modules',
        component: ModulesComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/logs',
        component: LogsComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'moderation' }
      },
      {
        path: 'guilds/:id/reaction-roles',
        component: ReactionRolesComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/subscription',
        component: SubscriptionComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/staff',
        component: StaffComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      { path: 'admin', component: AdminHomeComponent, canActivate: [AdminGuard] },
      { path: 'admin/guilds', component: AdminGuildsComponent, canActivate: [AdminGuard] },
      { path: 'admin/users', component: AdminUsersComponent, canActivate: [AdminGuard] },
      {
        path: 'admin/upgrade-requests',
        component: AdminUpgradeRequestsComponent,
        canActivate: [AdminGuard]
      },
      { path: '', redirectTo: 'servers', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'servers' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
