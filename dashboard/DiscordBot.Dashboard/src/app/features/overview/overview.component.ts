import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildService } from '../../core/services/guild.service';
import { ToastService } from '../../core/services/toast.service';
import { GuildOverview } from '../../core/models/guild.models';
import { GuildModule } from '../../core/models/module.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-overview',
  templateUrl: './overview.component.html',
  styleUrls: ['./overview.component.css']
})
export class OverviewComponent implements OnInit {
  guildId = '';
  overview: GuildOverview | null = null;
  modules: GuildModule[] = [];
  loading = true;
  syncing = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private guildService: GuildService,
    private auth: AuthService,
    private guildContext: GuildContextService,
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  get discordServerUrl(): string | null {
    return this.guildContext.discordServerUrl;
  }

  ngOnInit(): void {
    this.guildId = this.route.snapshot.paramMap.get('id') ?? '';

    if (!this.guildId) {
      this.router.navigate(['/servers']);
      return;
    }

    this.guildContext.ensureGuild(this.guildId, this.guildService);
    this.loadOverview();
  }

  loadOverview(): void {
    this.loading = true;
    this.error = '';

    forkJoin({
      overview: this.guildService.getOverview(this.guildId),
      modules: this.guildService.getModules(this.guildId)
    }).subscribe({
      next: ({ overview, modules }) => {
        this.overview = overview;
        this.modules = modules;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err.status === 401) {
          this.handleAuthError();
        } else {
          const message = getApiErrorMessage(err, this.translate.instant('errors.loadOverview'));
          this.error = message;
          this.toast.error(message);
        }
      }
    });
  }

  syncDiscordData(): void {
    this.syncing = true;

    this.guildService.requestResourceSync(this.guildId).subscribe({
      next: response => {
        this.syncing = false;
        this.toast.success(`✔ ${response.message}`);
        setTimeout(() => this.loadOverview(), 5000);
      },
      error: err => {
        this.syncing = false;
        if (err.status === 401) {
          this.handleAuthError();
        } else {
          this.toast.error(getApiErrorMessage(err, this.translate.instant('errors.syncFailed')));
        }
      }
    });
  }

  goToSettings(): void {
    this.router.navigate(['/guilds', this.guildId, 'settings']);
  }

  goToTickets(): void {
    this.router.navigate(['/guilds', this.guildId, 'tickets']);
  }

  goToModules(): void {
    this.router.navigate(['/guilds', this.guildId, 'modules']);
  }

  goToSubscription(): void {
    this.router.navigate(['/guilds', this.guildId, 'subscription']);
  }

  formatLastSync(value?: string | null): string {
    if (!value) {
      return '';
    }
    return new Date(value).toLocaleString();
  }

  moduleEnabled(moduleKey: string): boolean {
    const module = this.modules.find(item => item.key === moduleKey);
    if (!module) {
      return false;
    }

    return module.effectiveEnabled ?? (module.isEnabled && module.allowedByPlan);
  }

  featureStatus(enabled: boolean): string {
    return enabled ? 'common.enabled' : 'common.disabled';
  }

  featureClass(enabled: boolean): string {
    return enabled ? 'status-on' : 'status-off';
  }

  private handleAuthError(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
