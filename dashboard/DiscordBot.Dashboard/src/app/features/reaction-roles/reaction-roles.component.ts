import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { ReactionRolePanel } from '../../core/models/reaction-role.models';
import { DiscordChannel, DiscordRole, channelLabel, roleLabel } from '../../core/models/guild.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-reaction-roles',
  templateUrl: './reaction-roles.component.html',
  styleUrls: ['./reaction-roles.component.css']
})
export class ReactionRolesComponent implements OnInit {
  guildId = '';
  panels: ReactionRolePanel[] = [];
  channels: DiscordChannel[] = [];
  roles: DiscordRole[] = [];
  loading = true;
  error = '';
  deactivatingId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private guildService: GuildService,
    private guildContext: GuildContextService,
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.guildId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.guildId) {
      this.router.navigate(['/servers']);
      return;
    }

    this.guildContext.ensureGuild(this.guildId, this.guildService);
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    forkJoin({
      panels: this.guildService.getReactionRoles(this.guildId),
      channels: this.guildService.getChannels(this.guildId),
      roles: this.guildService.getRoles(this.guildId)
    }).subscribe({
      next: ({ panels, channels, roles }) => {
        this.panels = panels;
        this.channels = channels;
        this.roles = roles;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('errors.loadReactionRoles'));
        this.loading = false;
      }
    });
  }

  channelName(channelId: string): string {
    const channel = this.channels.find(c => c.discordChannelId === channelId);
    return channel ? channelLabel(channel) : channelId;
  }

  roleName(roleId: string): string {
    const role = this.roles.find(r => r.discordRoleId === roleId);
    return role ? roleLabel(role) : roleId;
  }

  deactivate(panel: ReactionRolePanel): void {
    if (!panel.isActive || this.deactivatingId) {
      return;
    }

    this.deactivatingId = panel.id;

    this.guildService.deactivateReactionRole(this.guildId, panel.id).subscribe({
      next: () => {
        panel.isActive = false;
        this.deactivatingId = null;
        this.toast.success(
          this.translate.instant('reactionRoles.deactivatedWithTitle', { title: panel.title })
        );
      },
      error: err => {
        this.deactivatingId = null;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('reactionRoles.deactivateError')));
      }
    });
  }

  isDeactivating(panel: ReactionRolePanel): boolean {
    return this.deactivatingId === panel.id;
  }
}
