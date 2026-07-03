import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-auto-role-notes',
  templateUrl: './auto-role-notes.component.html',
  styleUrls: ['./auto-role-notes.component.css']
})
export class AutoRoleNotesComponent {
  @Input() logsRoute = '';
}
