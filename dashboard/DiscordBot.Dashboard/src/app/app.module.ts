import { HttpClient } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { LoginComponent } from './features/auth/login/login.component';
import { CallbackComponent } from './features/auth/callback/callback.component';
import { DashboardLayoutComponent } from './features/layout/dashboard-layout.component';
import { ServersComponent } from './features/servers/servers.component';
import { OverviewComponent } from './features/overview/overview.component';
import { ModerationComponent } from './features/moderation/moderation.component';
import { SettingsComponent } from './features/settings/settings.component';
import { TicketsComponent } from './features/tickets/tickets.component';
import { TicketTranscriptComponent } from './features/tickets/ticket-transcript.component';
import { ModulesComponent } from './features/modules/modules.component';
import { LogsComponent } from './features/logs/logs.component';
import { ReactionRolesComponent } from './features/reaction-roles/reaction-roles.component';
import { SubscriptionComponent } from './features/subscription/subscription.component';
import { StaffComponent } from './features/staff/staff.component';
import { ProfileComponent } from './features/profile/profile.component';
import { ModerationSettingsComponent } from './features/moderation-settings/moderation-settings.component';
import { AdminHomeComponent } from './features/admin/admin-home/admin-home.component';
import { AdminGuildsComponent } from './features/admin/admin-guilds/admin-guilds.component';
import { AdminUsersComponent } from './features/admin/admin-users/admin-users.component';
import { AdminUpgradeRequestsComponent } from './features/admin/admin-upgrade-requests/admin-upgrade-requests.component';
import { AdminPlansComponent } from './features/admin/admin-plans/admin-plans.component';
import { ToastContainerComponent } from './shared/toast-container/toast-container.component';
import { OnboardingChecklistComponent } from './shared/onboarding-checklist/onboarding-checklist.component';
import { UiIconComponent } from './shared/ui/ui-icon/ui-icon.component';
import { LanguageSwitcherComponent } from './shared/ui/language-switcher/language-switcher.component';
import { ProfileMenuComponent } from './shared/ui/profile-menu/profile-menu.component';
import { ServerSwitcherComponent } from './shared/ui/server-switcher/server-switcher.component';
import { BreadcrumbsComponent } from './shared/ui/breadcrumbs/breadcrumbs.component';
import { EmptyStateComponent } from './shared/ui/empty-state/empty-state.component';
import { LoadingStateComponent } from './shared/ui/loading-state/loading-state.component';
import { MemberSelectComponent } from './shared/ui/member-select/member-select.component';

export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    CallbackComponent,
    DashboardLayoutComponent,
    ServersComponent,
    OverviewComponent,
    SettingsComponent,
    TicketsComponent,
    TicketTranscriptComponent,
    ModerationComponent,
    ModulesComponent,
    LogsComponent,
    ReactionRolesComponent,
    SubscriptionComponent,
    StaffComponent,
    ProfileComponent,
    ModerationSettingsComponent,
    AdminHomeComponent,
    AdminGuildsComponent,
    AdminUsersComponent,
    AdminUpgradeRequestsComponent,
    AdminPlansComponent,
    OnboardingChecklistComponent,
    ToastContainerComponent,
    UiIconComponent,
    LanguageSwitcherComponent,
    ProfileMenuComponent,
    ServerSwitcherComponent,
    BreadcrumbsComponent,
    EmptyStateComponent,
    LoadingStateComponent,
    MemberSelectComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    ReactiveFormsModule,
    FormsModule,
    AppRoutingModule,
    TranslateModule.forRoot({
      defaultLanguage: 'en',
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      }
    })
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
