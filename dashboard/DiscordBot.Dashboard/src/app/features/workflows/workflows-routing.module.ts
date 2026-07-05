import { NgModule } from '@angular/core'; import { RouterModule, Routes } from '@angular/router'; import { WorkflowsComponent } from './workflows.component';
@NgModule({ imports: [RouterModule.forChild([{ path: '', component: WorkflowsComponent }])], exports: [RouterModule] }) export class WorkflowsRoutingModule { }
