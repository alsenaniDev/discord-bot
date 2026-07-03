import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { UiIconComponent } from './ui/ui-icon/ui-icon.component';
import { LanguageSwitcherComponent } from './ui/language-switcher/language-switcher.component';
import { ProfileMenuComponent } from './ui/profile-menu/profile-menu.component';
import { ServerSwitcherComponent } from './ui/server-switcher/server-switcher.component';
import { BreadcrumbsComponent } from './ui/breadcrumbs/breadcrumbs.component';
import { EmptyStateComponent } from './ui/empty-state/empty-state.component';
import { LoadingStateComponent } from './ui/loading-state/loading-state.component';
import { MemberSelectComponent } from './ui/member-select/member-select.component';
import { PageWorkspaceHeroComponent } from './ui/page-workspace-hero/page-workspace-hero.component';
import { SectionHeaderComponent } from './ui/section-header/section-header.component';
import { StatusBadgeComponent } from './ui/status-badge/status-badge.component';
import { ErrorStateComponent } from './ui/error-state/error-state.component';
import { PageNoticeComponent } from './ui/page-notice/page-notice.component';

@NgModule({
  declarations: [
    UiIconComponent,
    LanguageSwitcherComponent,
    ProfileMenuComponent,
    ServerSwitcherComponent,
    BreadcrumbsComponent,
    EmptyStateComponent,
    LoadingStateComponent,
    MemberSelectComponent,
    PageWorkspaceHeroComponent,
    SectionHeaderComponent,
    StatusBadgeComponent,
    ErrorStateComponent,
    PageNoticeComponent
  ],
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule
  ],
  exports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    UiIconComponent,
    LanguageSwitcherComponent,
    ProfileMenuComponent,
    ServerSwitcherComponent,
    BreadcrumbsComponent,
    EmptyStateComponent,
    LoadingStateComponent,
    MemberSelectComponent,
    PageWorkspaceHeroComponent,
    SectionHeaderComponent,
    StatusBadgeComponent,
    ErrorStateComponent,
    PageNoticeComponent
  ]
})
export class SharedUiModule {}
