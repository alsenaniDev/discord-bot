import { NgModule } from '@angular/core';
import { StaffComponent } from './staff.component';
import { StaffFilterBarComponent } from './staff-filter-bar/staff-filter-bar.component';
import { StaffRoleCardComponent } from './staff-role-card/staff-role-card.component';
import { StaffRoleEditorComponent } from './staff-role-editor/staff-role-editor.component';
import { StaffDetailPanelComponent } from './staff-detail-panel/staff-detail-panel.component';
import { StaffRoutingModule } from './staff-routing.module';
import { SharedUiModule } from '../../shared/shared-ui.module';

@NgModule({
  declarations: [
    StaffComponent,
    StaffFilterBarComponent,
    StaffRoleCardComponent,
    StaffRoleEditorComponent,
    StaffDetailPanelComponent
  ],
  imports: [
    SharedUiModule,
    StaffRoutingModule
  ]
})
export class StaffModule {}
