import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildService } from '../../core/services/guild.service';
import { OnboardingService } from '../../core/services/onboarding.service';
import { ToastService } from '../../core/services/toast.service';
import { GuildSummary } from '../../core/models/guild.models';
import { OnboardingStatus, emptyChecklist } from '../../core/models/onboarding.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-servers',
  templateUrl: './servers.component.html',
  styleUrls: ['./servers.component.css']
})
export class ServersComponent implements OnInit {
  guilds: GuildSummary[] = [];
  onboarding: OnboardingStatus | null = null;
  loading = true;
  refreshing = false;
  error = '';

  constructor(
    private guildService: GuildService,
    private onboardingService: OnboardingService,
    private auth: AuthService,
    private guildContext: GuildContextService,
    private toast: ToastService,
    private router: Router,
    private translate: TranslateService
  ) {}

  get showOnboarding(): boolean {
    return !this.loading && !this.error && this.guilds.length === 0;
  }

  get botInviteUrl(): string {
    return this.onboarding?.botInviteUrl ?? '';
  }

  get onboardingChecklist() {
    return emptyChecklist();
  }

  ngOnInit(): void {
    this.guildContext.clearGuild();
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    forkJoin({
      guilds: this.guildService.getGuilds(),
      onboarding: this.onboardingService.getStatus()
    }).subscribe({
      next: ({ guilds, onboarding }) => {
        this.guilds = guilds;
        this.onboarding = onboarding;
        this.loading = false;
        this.refreshing = false;
      },
      error: err => {
        this.loading = false;
        this.refreshing = false;
        if (err.status === 401) {
          this.handleAuthError();
        } else {
          const message = getApiErrorMessage(err, this.translate.instant('errors.loadServers'));
          this.error = message;
          this.toast.error(message);
        }
      }
    });
  }

  refreshOnboarding(): void {
    this.refreshing = true;
    this.loadData();
  }

  checklistForGuild(guildId: string) {
    return this.onboarding?.guilds.find(g => g.guildId === guildId)?.checklist ?? null;
  }

  openOverview(guild: GuildSummary): void {
    this.guildContext.selectGuild(guild);
    this.router.navigate(['/guilds', guild.id, 'overview']);
  }

  openSettings(guild: GuildSummary): void {
    this.guildContext.selectGuild(guild);
    this.router.navigate(['/guilds', guild.id, 'settings']);
  }

  openTickets(guild: GuildSummary): void {
    this.guildContext.selectGuild(guild);
    this.router.navigate(['/guilds', guild.id, 'tickets']);
  }

  discordUrl(guild: GuildSummary): string {
    return `https://discord.com/channels/${guild.discordGuildId}`;
  }

  private handleAuthError(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
