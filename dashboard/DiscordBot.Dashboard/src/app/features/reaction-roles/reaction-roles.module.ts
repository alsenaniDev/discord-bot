import { NgModule } from '@angular/core';
import { ReactionRolesComponent } from './reaction-roles.component';
import { ReactionRolesFilterBarComponent } from './reaction-roles-filter-bar/reaction-roles-filter-bar.component';
import { ReactionRolesPanelCardComponent } from './reaction-roles-panel-card/reaction-roles-panel-card.component';
import { ReactionRolesDetailPanelComponent } from './reaction-roles-detail-panel/reaction-roles-detail-panel.component';
import { ReactionRolesRoutingModule } from './reaction-roles-routing.module';
import { SharedUiModule } from '../../shared/shared-ui.module';

@NgModule({
  declarations: [
    ReactionRolesComponent,
    ReactionRolesFilterBarComponent,
    ReactionRolesPanelCardComponent,
    ReactionRolesDetailPanelComponent
  ],
  imports: [
    SharedUiModule,
    ReactionRolesRoutingModule
  ]
})
export class ReactionRolesModule {}
