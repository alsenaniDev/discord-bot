import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PanelsComponent } from './panels.component';
const routes: Routes = [{ path: '', component: PanelsComponent }];
@NgModule({ imports: [RouterModule.forChild(routes)], exports: [RouterModule] })
export class PanelsRoutingModule { }
