import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildService } from '../../core/services/guild.service';
import { GuildAccessService } from '../../core/services/guild-access.service';
import { MissionControlHeaderService } from '../../core/services/mission-control-header.service';
import { GuildSummary } from '../../core/models/guild.models';
import { MissionControlHeaderState } from '../../core/models/mission-control.models';
import { GuildAccess } from '../../core/models/staff.models';
import { UserProfile } from '../../core/models/auth.models';
import { BreadcrumbItem } from '../../shared/ui/breadcrumbs/breadcrumbs.component';

@Component({
  selector: 'app-dashboard-layout',
  templateUrl: './dashboard-layout.component.html',
  styleUrls: ['./dashboard-layout.component.css']
})
export class DashboardLayoutComponent implements OnInit, OnDestroy {
  user: UserProfile | null = null;
  guilds: GuildSummary[] = [];
  selectedGuild: GuildSummary | null = null;
  sidebarOpen = false;
  pageTitleKey = 'titles.servers';
  pageTitleText = '';
  pageSubtitleKey = 'titles.serversSubtitle';
  pageSubtitleParams: Record<string, string> = {};
  breadcrumbs: BreadcrumbItem[] = [];
  usesWorkspaceHero = false;
  notificationsOpen = false;
  guildAccess: GuildAccess | null = null;
  missionControlHeader: MissionControlHeaderState = {
    visible: false,
    loading: false,
    model: null
  };

  private routerSub?: Subscription;
  private guildSub?: Subscription;
  private accessSub?: Subscription;
  private missionHeaderSub?: Subscription;

  constructor(
    private router: Router,
    private auth: AuthService,
    private guildService: GuildService,
    private guildContext: GuildContextService,
    private guildAccessService: GuildAccessService,
    private missionControlHeaderService: MissionControlHeaderService
  ) { }

  get showMissionControlStatus(): boolean {
    return this.missionControlHeader.visible;
  }

  get discordServerUrl(): string | null {
    return this.guildContext.discordServerUrl;
  }

  get showGuildNav(): boolean {
    return !!this.selectedGuild;
  }

  get canManageGuild(): boolean {
    return !!this.guildAccess?.canManageSettings;
  }

  get canAccessModeration(): boolean {
    return !!this.guildAccess?.canAccessModeration;
  }

  ngOnInit(): void {
    this.auth.getCurrentUser().subscribe({
      next: user => {
        this.user = user;
        this.updateTitles();
      },
      error: () => {
        this.auth.logout();
        this.router.navigate(['/login']);
      }
    });
    this.guildService.getGuilds().subscribe({ next: guilds => { this.guilds = guilds; } });

    this.guildSub = this.guildContext.selectedGuild$.subscribe(guild => {
      this.selectedGuild = guild;
      this.loadGuildAccess(guild);
      this.updateTitles();
    });

    this.routerSub = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.syncGuildFromRoute();
        this.updateTitles();
        this.sidebarOpen = false;
      });

    this.syncGuildFromRoute();
    this.updateTitles();

    this.missionHeaderSub = this.missionControlHeaderService.state$.subscribe(state => {
      this.missionControlHeader = state;
    });
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
    this.guildSub?.unsubscribe();
    this.accessSub?.unsubscribe();
    this.missionHeaderSub?.unsubscribe();
  }

  toggleSidebar(): void { this.sidebarOpen = !this.sidebarOpen; }
  closeSidebar(): void { this.sidebarOpen = false; }

  logout(): void {
    this.guildContext.clearGuild();
    this.guildAccessService.clearAccess();
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  private loadGuildAccess(guild: GuildSummary | null): void {
    this.accessSub?.unsubscribe();
    this.guildAccess = null;

    if (!guild) {
      return;
    }

    this.accessSub = this.guildAccessService.loadAccess(guild.id).subscribe({
      next: access => { this.guildAccess = access; },
      error: () => { this.guildAccess = null; }
    });
  }

  private syncGuildFromRoute(): void {
    const match = this.router.url.match(/\/guilds\/([^/]+)/);
    if (!match || this.router.url.startsWith('/admin')) {
      this.guildContext.clearGuild();
      return;
    }
    this.guildContext.ensureGuild(match[1], this.guildService);
  }

  private updateTitles(): void {
    const url = this.router.url;
    const guildName = this.selectedGuild?.name ?? '';
    const guildId = this.selectedGuild?.id;

    if (url.startsWith('/guilds/') && url.includes('/overview')) {
      this.setGuildPage('titles.overview', '', guildName, guildId, 'common.overview');
      return;
    }
    if (url.includes('/settings') && !url.includes('/moderation/settings')) {
      this.setGuildPage('titles.settings', 'titles.settingsSubtitle', guildName, guildId, 'common.settings');
      return;
    }
    if (url.includes('/panels')) {
      this.setGuildPage('panels.title', 'panels.subtitle', guildName, guildId, 'nav.panels');
      return;
    }
    if (url.includes('/workflows')) {
      this.setGuildPage('workflows.title', 'workflows.subtitle', guildName, guildId, 'nav.workflows');
      return;
    }
    if (url.includes('/music')) {
      this.setGuildPage('music.title', 'music.subtitle', guildName, guildId, 'nav.music');
      return;
    }
    if (url.includes('/games') && url.startsWith('/guilds/')) {
      this.setGuildPage('titles.games', 'titles.gamesSubtitle', guildName, guildId, 'nav.games');
      return;
    }
    if (url.includes('/profile')) {
      this.setGuildPage('titles.profile', 'titles.profileSubtitle', guildName, guildId, 'nav.profile');
      return;
    }
    if (url.includes('/moderation/settings')) {
      this.setGuildPage('titles.moderationSettings', 'titles.moderationSettingsSubtitle', guildName, guildId, 'nav.moderationSettings');
      return;
    }
    if (url.includes('/tickets/') && url.includes('/transcript')) {
      this.setGuildPage('titles.ticketTranscript', 'titles.ticketTranscriptSubtitle', guildName, guildId, 'common.tickets');
      return;
    }
    if (url.includes('/tickets')) {
      this.setGuildPage('titles.tickets', 'titles.ticketsSubtitle', guildName, guildId, 'common.tickets');
      return;
    }
    if (url.includes('/moderation')) {
      this.setGuildPage('titles.moderation', 'titles.moderationSubtitle', guildName, guildId, 'nav.moderation');
      return;
    }
    if (url.includes('/modules')) {
      this.setGuildPage('titles.modules', 'titles.modulesSubtitle', guildName, guildId, 'common.modules');
      return;
    }
    if (url.includes('/subscription')) {
      this.setGuildPage('titles.subscription', 'titles.subscriptionSubtitle', guildName, guildId, 'common.subscription');
      return;
    }
    if (url.includes('/logs')) {
      this.setGuildPage('titles.logs', 'titles.logsSubtitle', guildName, guildId, 'nav.logs');
      return;
    }
    if (url.startsWith('/admin/upgrade-requests')) {
      this.setPage('titles.adminUpgradeRequests', 'titles.adminUpgradeRequestsSubtitle', '', [
        { label: 'nav.platformAdmin', link: '/admin' },
        { label: 'nav.upgradeRequests' }
      ]);
      return;
    }
    if (url.startsWith('/admin/plans')) {
      this.setPage('titles.adminPlans', 'titles.adminPlansSubtitle', '', [
        { label: 'nav.platformAdmin', link: '/admin' },
        { label: 'nav.adminPlans' }
      ]);
      return;
    }
    if (url.startsWith('/admin/games')) {
      this.setPage('titles.adminGames', 'titles.adminGamesSubtitle', '', [
        { label: 'nav.platformAdmin', link: '/admin' },
        { label: 'nav.gamesCatalog' }
      ]);
      return;
    }
    if (url.includes('/reaction-roles')) {
      this.setGuildPage('titles.reactionRoles', 'titles.reactionRolesSubtitle', guildName, guildId, 'nav.reactionRoles');
      return;
    }
    if (url.includes('/staff')) {
      this.setGuildPage('titles.staff', 'titles.staffSubtitle', guildName, guildId, 'nav.staff');
      return;
    }
    if (url === '/admin' || url.startsWith('/admin?')) {
      this.setPage('titles.adminOverview', 'titles.adminOverviewSubtitle', '', [
        { label: 'nav.platformAdmin' },
        { label: 'nav.adminOverview' }
      ]);
      return;
    }
    if (url.startsWith('/admin/guilds')) {
      this.setPage('titles.adminGuilds', 'titles.adminGuildsSubtitle', '', [
        { label: 'nav.platformAdmin', link: '/admin' },
        { label: 'nav.allGuilds' }
      ]);
      return;
    }
    if (url.startsWith('/admin/users')) {
      this.setPage('titles.adminUsers', 'titles.adminUsersSubtitle', '', [
        { label: 'nav.platformAdmin', link: '/admin' },
        { label: 'nav.users' }
      ]);
      return;
    }

    this.pageTitleKey = 'titles.servers';
    this.pageTitleText = '';
    this.pageSubtitleKey = this.user ? 'titles.serversSignedIn' : 'titles.serversSubtitle';
    this.pageSubtitleParams = this.user
      ? { name: this.user.globalName || this.user.username }
      : {};
    this.breadcrumbs = [{ label: 'nav.servers' }];
    this.usesWorkspaceHero = false;
  }

  private setGuildPage(
    titleKey: string,
    subtitleKey: string,
    guildName: string,
    guildId: string | undefined,
    pageLabel: string
  ): void {
    const crumbs: BreadcrumbItem[] = [];
    if (guildId && guildName) {
      crumbs.push({
        label: guildName,
        link: ['/guilds', guildId, 'overview'],
        translate: false
      });
    }
    crumbs.push({ label: pageLabel });
    this.setPage(titleKey, subtitleKey, guildName, crumbs, !!guildName);
    this.usesWorkspaceHero = true;
  }

  private setPage(
    titleKey: string,
    subtitleKey: string,
    guildFallback: string,
    crumbs: BreadcrumbItem[],
    useGuildNameAsTitle = false
  ): void {
    this.pageTitleText = useGuildNameAsTitle && guildFallback ? guildFallback : '';
    this.pageTitleKey = this.pageTitleText ? '' : titleKey;
    this.pageSubtitleKey = subtitleKey;
    this.pageSubtitleParams = {};
    this.breadcrumbs = crumbs;
    this.usesWorkspaceHero = false;
  }
}
