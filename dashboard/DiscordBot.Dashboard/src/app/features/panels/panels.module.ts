import { NgModule } from '@angular/core';
import { SharedUiModule } from '../../shared/shared-ui.module';
import { PanelsComponent } from './panels.component';
import { PanelsRoutingModule } from './panels-routing.module';
@NgModule({ declarations: [PanelsComponent], imports: [SharedUiModule, PanelsRoutingModule] })
export class PanelsModule {}
