import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin, Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AnalyticsService } from '../../core/services/analytics.service';
import { ContextDrawerMapperService } from '../../core/services/context-drawer-mapper.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildService } from '../../core/services/guild.service';
import { ActivityTimelineMapperService } from '../../core/services/activity-timeline-mapper.service';
import { CommunityPulseMapperService } from '../../core/services/community-pulse-mapper.service';
import { MissionControlHeaderService } from '../../core/services/mission-control-header.service';
import { MissionDismissService } from '../../core/services/mission-dismiss.service';
import { MissionMapperService } from '../../core/services/mission-mapper.service';
import { ToastService } from '../../core/services/toast.service';
import { GuildOverview, GuildOverviewExperience } from '../../core/models/guild.models';
import {
  ActivityTimelineModel,
  CommunityPulseModel,
  ContextDrawerModel,
  MissionCardModel,
  PulseMetricModel
} from '../../core/models/mission-control.models';
import { GuildModule } from '../../core/models/module.models';
import { GuildAccess } from '../../core/models/staff.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroIconName,
  PageWorkspaceHeroStat
} from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';

@Component({
  selector: 'app-overview',
  templateUrl: './overview.component.html',
  styleUrls: ['./overview.component.css']
})
export class OverviewComponent implements OnInit, OnDestroy {
  guildId = '';
  overview: GuildOverview | null = null;
  experience: GuildOverviewExperience | null = null;
  modules: GuildModule[] = [];
  access: GuildAccess | null = null;
  missionCard: MissionCardModel | null = null;
  communityPulse: CommunityPulseModel | null = null;
  activityTimeline: ActivityTimelineModel | null = null;
  contextDrawer: ContextDrawerModel | null = null;
  contextDrawerOpen = false;
  loading = true;
  syncing = false;
  error = '';
  private userId?: string;
  private authSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private guildService: GuildService,
    private auth: AuthService,
    private guildContext: GuildContextService,
    private toast: ToastService,
    private translate: TranslateService,
    private analytics: AnalyticsService,
    private missionMapper: MissionMapperService,
    private communityPulseMapper: CommunityPulseMapperService,
    private activityTimelineMapper: ActivityTimelineMapperService,
    private contextDrawerMapper: ContextDrawerMapperService,
    private missionDismiss: MissionDismissService,
    private missionControlHeader: MissionControlHeaderService
  ) {}

  ngOnInit(): void {
    this.guildId = this.route.snapshot.paramMap.get('id') ?? '';

    if (!this.guildId) {
      this.router.navigate(['/servers']);
      return;
    }

    this.authSub = this.auth.getCurrentUser().subscribe({
      next: user => { this.userId = user.id; },
      error: () => { this.userId = undefined; }
    });

    this.guildContext.ensureGuild(this.guildId, this.guildService);
    this.missionControlHeader.showLoading();
    this.missionCard = this.missionMapper.createLoadingMission();
    this.communityPulse = this.communityPulseMapper.createLoadingPulse();
    this.activityTimeline = this.activityTimelineMapper.createLoadingTimeline();
    this.contextDrawer = this.contextDrawerMapper.createLoadingDrawer();
    this.loadOverview();
  }

  ngOnDestroy(): void {
    this.authSub?.unsubscribe();
    this.missionControlHeader.clear();
  }

  loadOverview(): void {
    this.loading = true;
    this.error = '';
    this.missionControlHeader.showLoading();
    this.missionCard = this.missionMapper.createLoadingMission();
    this.communityPulse = this.communityPulseMapper.createLoadingPulse();
    this.activityTimeline = this.activityTimelineMapper.createLoadingTimeline();
    this.contextDrawer = this.contextDrawerMapper.createLoadingDrawer();

    forkJoin({
      overview: this.guildService.getOverview(this.guildId),
      modules: this.guildService.getModules(this.guildId),
      access: this.guildService.getGuildAccess(this.guildId)
    }).subscribe({
      next: ({ overview, modules, access }) => {
        this.overview = overview;
        this.experience = overview.experience ?? null;
        this.modules = modules;
        this.access = access;
        this.loading = false;
        this.updateMissionControl();

        this.analytics.track('OverviewViewed', { guildId: this.guildId });
        if (this.experience?.activation) {
          this.analytics.track('ActivationProgressViewed', {
            guildId: this.guildId,
            progressPercent: this.experience.activation.progressPercent
          });
        }
        if (this.experience?.health) {
          this.analytics.track('HealthCardViewed', {
            guildId: this.guildId,
            score: this.experience.health.score,
            level: this.experience.health.level
          });
        }
      },
      error: err => {
        this.loading = false;
        this.missionControlHeader.clear();
        this.missionCard = null;
        this.communityPulse = null;
        this.activityTimeline = null;
        this.contextDrawer = null;
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

  onMissionCta(): void {
    if (!this.missionCard?.cta) {
      return;
    }

    const { action, route } = this.missionCard.cta;

    if (action === 'sync') {
      this.syncDiscordData();
      return;
    }

    if (action === 'external-discord') {
      const url = this.guildContext.discordServerUrl;
      if (url) {
        window.open(url, '_blank', 'noopener,noreferrer');
      }
      return;
    }

    if (route) {
      this.navigate(route);
    }
  }

  onMissionDismiss(): void {
    if (!this.missionCard || !this.missionCard.dismissible) {
      return;
    }

    this.missionDismiss.snoozeSevenDays(this.missionCard.missionId, this.guildId, this.userId);
    this.toast.success(this.translate.instant('overview.v2.dismiss.snoozed'));
    this.updateMissionControl();
  }

  onMissionRetry(): void {
    this.loadOverview();
  }

  get workspaceHeroIcon(): PageWorkspaceHeroIconName {
    const icon = this.missionCard?.icon ?? 'overview';
    return icon as PageWorkspaceHeroIconName;
  }

  get workspaceHeroTitle(): string {
    if (!this.missionCard) {
      return this.translate.instant('titles.overview');
    }

    return this.translate.instant(this.missionCard.titleKey, this.missionCard.descriptionParams);
  }

  get workspaceHeroDescription(): string {
    return this.translate.instant('workspaceHero.overview.description');
  }

  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    if (!this.communityPulse?.metrics?.length) {
      return [];
    }

    return this.communityPulse.metrics.slice(0, 4).map(metric => ({
      label: this.translate.instant(metric.labelKey),
      value: this.pulseMetricValue(metric),
      compact: metric.id === 'modules' || metric.id === 'setup'
    }));
  }

  get workspaceHeroFooter(): string {
    if (!this.missionCard) {
      return '';
    }

    if (this.missionCard.missionId === 'EverythingOperational') {
      return this.translate.instant('workspaceHero.overview.footer.operational');
    }

    if (this.missionCard.missionId === 'StaffCalm') {
      return this.translate.instant('workspaceHero.overview.footer.staffCalm');
    }

    if (this.missionCard.showProgress && this.missionCard.progressPhases?.length) {
      return this.missionCard.progressPhases
        .map(phase => this.translate.instant(phase.labelKey))
        .join(' · ');
    }

    return this.translate.instant(this.missionCard.descriptionKey, this.missionCard.descriptionParams);
  }

  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction | null {
    if (!this.missionCard) {
      return null;
    }

    if (this.missionCard.error) {
      return {
        label: this.translate.instant('overview.v2.mission.error.retry')
      };
    }

    if (!this.missionCard.cta) {
      return null;
    }

    return {
      label: this.translate.instant(this.missionCard.cta.labelKey),
      loading: this.missionCard.loading
    };
  }

  private pulseMetricValue(metric: PulseMetricModel): string {
    if (metric.health) {
      const level = this.translate.instant(metric.health.levelLabelKey);
      return this.translate.instant('overview.v2.pulse.healthValue', {
        score: metric.health.score,
        level
      });
    }

    if (!metric.valueKey) {
      return this.translate.instant('common.emptyValue');
    }

    return this.translate.instant(metric.valueKey, metric.valueParams);
  }

  private updateMissionControl(): void {
    if (!this.overview || !this.experience || !this.access) {
      return;
    }

    this.missionControlHeader.setStatus(
      this.missionMapper.mapStatusStrip(
        this.experience,
        this.access,
        this.syncing,
        this.overview.resourcesSyncedAt
      )
    );

    this.missionCard = this.missionMapper.mapMissionCard(
      {
        overview: this.overview,
        experience: this.experience,
        access: this.access,
        guildId: this.guildId
      },
      this.userId
    );

    this.communityPulse = this.communityPulseMapper.mapPulse({
      overview: this.overview,
      experience: this.experience,
      modules: this.modules,
      access: this.access
    });

    this.activityTimeline = this.activityTimelineMapper.mapTimeline({
      items: this.experience.recentActivity,
      guildId: this.guildId,
      access: this.access
    });

    this.contextDrawer = this.contextDrawerMapper.mapDrawer({
      guildId: this.guildId,
      modules: this.modules,
      experience: this.experience,
      access: this.access
    });
  }

  onActivityRowClick(route: string): void {
    this.router.navigateByUrl(route);
  }

  onActivityViewAll(): void {
    this.navigate('logs');
  }

  onActivityRetry(): void {
    this.loadOverview();
  }

  onDrawerNavigate(route: string): void {
    this.router.navigateByUrl(route);
  }

  syncDiscordData(): void {
    this.syncing = true;
    this.updateMissionControlHeaderOnly();

    this.guildService.requestResourceSync(this.guildId).subscribe({
      next: response => {
        this.syncing = false;
        this.toast.success(response.message);
        setTimeout(() => this.loadOverview(), 5000);
      },
      error: err => {
        this.syncing = false;
        this.updateMissionControlHeaderOnly();
        if (err.status === 401) {
          this.handleAuthError();
        } else {
          this.toast.error(getApiErrorMessage(err, this.translate.instant('errors.syncFailed')));
        }
      }
    });
  }

  private updateMissionControlHeaderOnly(): void {
    if (!this.experience || !this.access) {
      return;
    }

    this.missionControlHeader.setStatus(
      this.missionMapper.mapStatusStrip(
        this.experience,
        this.access,
        this.syncing,
        this.overview?.resourcesSyncedAt
      )
    );
  }

  navigate(route: string): void {
    if (route.startsWith('/')) {
      this.router.navigateByUrl(route);
      return;
    }

    this.router.navigate(['/guilds', this.guildId, route]);
  }

  private handleAuthError(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
