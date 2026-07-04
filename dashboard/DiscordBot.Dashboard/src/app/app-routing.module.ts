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
import { TicketsComponent } from './features/tickets/tickets.component';
import { ModulesComponent } from './features/modules/modules.component';
import { LogsComponent } from './features/logs/logs.component';
import { ProfileComponent } from './features/profile/profile.component';
import { ModerationSettingsComponent } from './features/moderation-settings/moderation-settings.component';
import { AdminGuard } from './core/guards/admin.guard';

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
        loadChildren: () =>
          import('./features/settings/settings.module').then(m => m.SettingsModule),
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/panels',
        loadChildren: () => import('./features/panels/panels.module').then(m => m.PanelsModule),
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/tickets/:ticketId/transcript',
        loadChildren: () =>
          import('./features/tickets/ticket-transcript.module').then(m => m.TicketTranscriptModule),
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'moderation' }
      },
      {
        path: 'guilds/:id/tickets',
        component: TicketsComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'moderation' }
      },
      {
        path: 'guilds/:id/moderation/settings',
        component: ModerationSettingsComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' },
        pathMatch: 'full'
      },
      {
        path: 'guilds/:id/moderation',
        component: ModerationComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'moderation' },
        pathMatch: 'full'
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
        loadChildren: () =>
          import('./features/reaction-roles/reaction-roles.module').then(m => m.ReactionRolesModule),
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/subscription',
        loadChildren: () =>
          import('./features/subscription/subscription.module').then(m => m.SubscriptionModule),
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/profile',
        component: ProfileComponent,
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'guilds/:id/staff',
        loadChildren: () =>
          import('./features/staff/staff.module').then(m => m.StaffModule),
        canActivate: [GuildAccessGuard],
        data: { guildAccess: 'owner' }
      },
      {
        path: 'admin',
        loadChildren: () => import('./features/admin/admin.module').then(m => m.AdminModule),
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
export class AppRoutingModule {}
