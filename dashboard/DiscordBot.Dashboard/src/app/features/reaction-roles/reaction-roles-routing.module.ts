import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReactionRolesComponent } from './reaction-roles.component';

const routes: Routes = [
  { path: '', component: ReactionRolesComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ReactionRolesRoutingModule {}
