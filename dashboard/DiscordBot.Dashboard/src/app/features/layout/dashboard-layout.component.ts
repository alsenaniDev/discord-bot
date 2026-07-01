import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildService } from '../../core/services/guild.service';
import { GuildAccessService } from '../../core/services/guild-access.service';
import { GuildSummary } from '../../core/models/guild.models';
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
  notificationsOpen = false;
  guildAccess: GuildAccess | null = null;

  private routerSub?: Subscription;
  private guildSub?: Subscription;
  private accessSub?: Subscription;

  constructor(
    private router: Router,
    private auth: AuthService,
    private guildService: GuildService,
    private guildContext: GuildContextService,
    private guildAccessService: GuildAccessService
  ) {}

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
    this.auth.getCurrentUser().subscribe({ next: user => { this.user = user; this.updateTitles(); } });
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
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
    this.guildSub?.unsubscribe();
    this.accessSub?.unsubscribe();
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

    if (url.startsWith('/guilds/') && url.includes('/overview')) {
      this.setPage('titles.overview', 'titles.overviewSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'common.overview' }
      ], true);
      return;
    }
    if (url.includes('/settings')) {
      this.setPage('titles.settings', 'titles.settingsSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'common.settings' }
      ], true);
      return;
    }
    if (url.includes('/tickets')) {
      this.setPage('titles.tickets', 'titles.ticketsSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'common.tickets' }
      ], true);
      return;
    }
    if (url.includes('/moderation')) {
      this.setPage('titles.moderation', 'titles.moderationSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'nav.moderation' }
      ], true);
      return;
    }
    if (url.includes('/modules')) {
      this.setPage('titles.modules', 'titles.modulesSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'common.modules' }
      ], true);
      return;
    }
    if (url.includes('/subscription')) {
      this.setPage('titles.subscription', 'titles.subscriptionSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'common.subscription' }
      ], true);
      return;
    }
    if (url.includes('/logs')) {
      this.setPage('titles.logs', 'titles.logsSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'nav.logs' }
      ], true);
      return;
    }
    if (url.startsWith('/admin/upgrade-requests')) {
      this.setPage('titles.adminUpgradeRequests', 'titles.adminUpgradeRequestsSubtitle', '', [
        { label: 'nav.platformAdmin', link: '/admin' },
        { label: 'nav.upgradeRequests' }
      ]);
      return;
    }
    if (url.includes('/reaction-roles')) {
      this.setPage('titles.reactionRoles', 'titles.reactionRolesSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'nav.reactionRoles' }
      ], true);
      return;
    }
    if (url.includes('/staff')) {
      this.setPage('titles.staff', 'titles.staffSubtitle', guildName, [
        { label: guildName, link: ['/guilds', this.selectedGuild!.id, 'overview'], translate: false },
        { label: 'nav.staff' }
      ], true);
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
  }
}
