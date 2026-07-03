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
import { ActivityTimelineComponent } from './features/overview/mission-control/activity-timeline/activity-timeline.component';
import { ContextDrawerComponent } from './features/overview/mission-control/context-drawer/context-drawer.component';
import { StatusStripComponent } from './features/overview/mission-control/status-strip/status-strip.component';
import { ModerationComponent } from './features/moderation/moderation.component';
import { ModerationFilterBarComponent } from './features/moderation/moderation-filter-bar/moderation-filter-bar.component';
import { ModerationEntryCardComponent } from './features/moderation/moderation-entry-card/moderation-entry-card.component';
import { ModerationDetailPanelComponent } from './features/moderation/moderation-detail-panel/moderation-detail-panel.component';
import { TicketsComponent } from './features/tickets/tickets.component';
import { TicketsContextDrawerComponent } from './features/tickets/tickets-context-drawer/tickets-context-drawer.component';
import { TicketsFilterBarComponent } from './features/tickets/tickets-filter-bar/tickets-filter-bar.component';
import { TicketsQueueCardComponent } from './features/tickets/tickets-queue-card/tickets-queue-card.component';
import { ModulesComponent } from './features/modules/modules.component';
import { ModulesModuleCardComponent } from './features/modules/modules-module-card/modules-module-card.component';
import { LogsComponent } from './features/logs/logs.component';
import { LogsFilterBarComponent } from './features/logs/logs-filter-bar/logs-filter-bar.component';
import { LogsEntryCardComponent } from './features/logs/logs-entry-card/logs-entry-card.component';
import { LogsDetailPanelComponent } from './features/logs/logs-detail-panel/logs-detail-panel.component';
import { ProfileComponent } from './features/profile/profile.component';
import { ProfilePreviewComponent } from './features/profile/profile-preview/profile-preview.component';
import { ModerationSettingsComponent } from './features/moderation-settings/moderation-settings.component';
import { ToastContainerComponent } from './shared/toast-container/toast-container.component';
import { OnboardingChecklistComponent } from './shared/onboarding-checklist/onboarding-checklist.component';
import { SharedUiModule } from './shared/shared-ui.module';

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
    StatusStripComponent,
    ActivityTimelineComponent,
    ContextDrawerComponent,
    TicketsComponent,
    TicketsFilterBarComponent,
    TicketsQueueCardComponent,
    TicketsContextDrawerComponent,
    ModerationComponent,
    ModerationFilterBarComponent,
    ModerationEntryCardComponent,
    ModerationDetailPanelComponent,
    ModulesComponent,
    ModulesModuleCardComponent,
    LogsComponent,
    LogsFilterBarComponent,
    LogsEntryCardComponent,
    LogsDetailPanelComponent,
    ProfileComponent,
    ProfilePreviewComponent,
    ModerationSettingsComponent,
    OnboardingChecklistComponent,
    ToastContainerComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    ReactiveFormsModule,
    FormsModule,
    SharedUiModule,
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
