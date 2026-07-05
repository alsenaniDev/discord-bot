import { NgModule } from '@angular/core'; import { SharedUiModule } from '../../shared/shared-ui.module'; import { WorkflowsComponent } from './workflows.component'; import { WorkflowsRoutingModule } from './workflows-routing.module';
@NgModule({ declarations: [WorkflowsComponent], imports: [SharedUiModule, WorkflowsRoutingModule] }) export class WorkflowsModule { }
