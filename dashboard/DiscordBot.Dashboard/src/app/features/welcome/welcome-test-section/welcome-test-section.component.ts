import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-welcome-test-section',
  templateUrl: './welcome-test-section.component.html',
  styleUrls: ['./welcome-test-section.component.css']
})
export class WelcomeTestSectionComponent {
  @Input() channelLabel = '';
  @Input() enabled = false;
  @Input() previewReady = false;
}
