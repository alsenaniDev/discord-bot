import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-profile-preview',
  templateUrl: './profile-preview.component.html',
  styleUrls: ['./profile-preview.component.css']
})
export class ProfilePreviewComponent {
  @Input() iconUrl: string | null = null;
  @Input() displayName = '';
  @Input() description = '';
  @Input() communityType = '';
  @Input() supportMessage = '';
  @Input() websiteUrl = '';
  @Input() rulesUrl = '';
}
