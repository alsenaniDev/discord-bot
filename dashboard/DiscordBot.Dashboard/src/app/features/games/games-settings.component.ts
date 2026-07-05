import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { ToastService } from '../../core/services/toast.service';
import { DiscordChannel } from '../../core/models/guild.models';
import { GameLeaderboardEntry, GuildGame, UpdateGuildGameSetting } from '../../core/models/games.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import { PageWorkspaceHeroAction, PageWorkspaceHeroStat } from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';

@Component({ selector: 'app-games-settings', templateUrl: './games-settings.component.html', styleUrls: ['../settings/settings.component.css', './games-settings.component.css'] })
export class GamesSettingsComponent implements OnInit {
  guildId = ''; games: GuildGame[] = []; channels: DiscordChannel[] = []; leaderboard: GameLeaderboardEntry[] = []; loading = true; saving = false; savingGameId: string | null = null; error = '';
  form = this.fb.group({ isEnabled: [false], gamesChannelDiscordId: [''], autoPostPanel: [false] });
  get stats(): PageWorkspaceHeroStat[] { return [{ label: 'الحالة', value: this.form.value.isEnabled ? 'مفعّلة' : 'غير مفعّلة' }, { label: 'الألعاب المفعّلة', value: String(this.games.filter(x => x.isEnabledForGuild).length) }, { label: 'اللاعبون', value: String(this.leaderboard.length) }]; }
  get action(): PageWorkspaceHeroAction { return { label: 'حفظ الإعدادات', disabled: this.saving }; }
  constructor(private route: ActivatedRoute, private fb: FormBuilder, private api: GuildService, private toast: ToastService) { }
  ngOnInit(): void { this.guildId = this.route.snapshot.paramMap.get('id') ?? ''; this.load(); }
  load(): void {
    this.loading = true; this.error = '';
    forkJoin({ settings: this.api.getGamesSettings(this.guildId), games: this.api.getGuildGames(this.guildId), channels: this.api.getChannels(this.guildId), leaderboard: this.api.getGamesLeaderboard(this.guildId) }).subscribe({
      next: x => { this.form.patchValue({ isEnabled: x.settings.isEnabled, gamesChannelDiscordId: x.settings.gamesChannelDiscordId ?? '', autoPostPanel: x.settings.autoPostPanel }); this.games = x.games; this.channels = x.channels.filter(c => c.type === 0 || String(c.type).toLowerCase() === 'text'); this.leaderboard = x.leaderboard; this.loading = false; },
      error: e => { this.loading = false; this.error = getApiErrorMessage(e, 'تعذر تحميل إعدادات الألعاب.'); }
    });
  }
  saveSettings(): void {
    const x = this.form.getRawValue(); if (x.isEnabled && !x.gamesChannelDiscordId) { this.toast.error('حدد روم الألعاب قبل تفعيل الميزة.'); return; }
    this.saving = true; this.api.updateGamesSettings(this.guildId, { isEnabled: !!x.isEnabled, gamesChannelDiscordId: x.gamesChannelDiscordId || null, autoPostPanel: !!x.autoPostPanel }).subscribe({ next: value => { this.saving = false; this.form.patchValue({ ...value, gamesChannelDiscordId: value.gamesChannelDiscordId ?? '' }); this.toast.success('تم حفظ إعدادات مركز الألعاب.'); this.refreshGames(); }, error: e => { this.saving = false; this.toast.error(getApiErrorMessage(e, 'تعذر حفظ الإعدادات.')); } });
  }
  saveGame(game: GuildGame): void {
    const value: UpdateGuildGameSetting = { isEnabledForGuild: game.isEnabledForGuild, pointsEnabled: game.pointsEnabled, pointsPerWin: Number(game.pointsPerWin), cooldownSeconds: Number(game.cooldownSeconds), maxPlaysPerDay: Number(game.maxPlaysPerDay), publishResultAfterGame: game.publishResultAfterGame, publishLeaderboardAfterGame: game.publishLeaderboardAfterGame, publishOnlyWins: game.publishOnlyWins };
    this.savingGameId = game.id; this.api.updateGuildGame(this.guildId, game.id, value).subscribe({ next: updated => { Object.assign(game, updated); this.savingGameId = null; this.toast.success(`تم حفظ إعدادات ${game.name}.`); }, error: e => { this.savingGameId = null; this.toast.error(getApiErrorMessage(e, 'تعذر حفظ إعدادات اللعبة.')); } });
  }
  private refreshGames(): void { this.api.getGuildGames(this.guildId).subscribe({ next: x => this.games = x }); }
}
