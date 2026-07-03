import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-welcome-discord-preview',
  templateUrl: './welcome-discord-preview.component.html'
})
export class WelcomeDiscordPreviewComponent {
  @Input() enabled = false;
  @Input() channelLabel = '';
  @Input() guildName = '';
  @Input() botName = '';
  @Input() previewMessage = '';
  @Input() footerText = '';
  @Input() embedTitle = '';
  @Input() authorName = '';
  @Input() authorAvatarUrl: string | null = null;
  @Input() previewReady = false;
}
