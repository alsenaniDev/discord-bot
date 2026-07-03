import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-reaction-roles-panel-card',
  templateUrl: './reaction-roles-panel-card.component.html',
  styleUrls: ['./reaction-roles-panel-card.component.css']
})
export class ReactionRolesPanelCardComponent {
  @Input() title = '';
  @Input() description = '';
  @Input() channelLabel = '';
  @Input() roleLabel = '';
  @Input() messageStatusLabel = '';
  @Input() messageLinked = false;
  @Input() active = false;
  @Input() selected = false;
  @Input() canOpen = false;
  @Input() canCopy = false;

  @Output() select = new EventEmitter<void>();
  @Output() configure = new EventEmitter<void>();
  @Output() copy = new EventEmitter<void>();
  @Output() open = new EventEmitter<void>();
}
