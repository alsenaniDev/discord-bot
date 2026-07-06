import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { ToastService } from '../../core/services/toast.service';
import { DiscordChannel } from '../../core/models/guild.models';
import { GameLeaderboardEntry, GuildGame, UpdateGuildGameSetting } from '../../core/models/games.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import { PageWorkspaceHeroAction, PageWorkspaceHeroStat } from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';

@Component({ selector: 'app-games-settings', templateUrl: './games-settings.component.html', styleUrls: ['../settings/settings.component.css', './games-settings.component.css'] })
export class GamesSettingsComponent implements OnInit {
  guildId = ''; games: GuildGame[] = []; channels: DiscordChannel[] = []; leaderboard: GameLeaderboardEntry[] = []; loading = true; saving = false; savingRoulette = false; savingGameId: string | null = null; error = '';
  form = this.fb.group({ isEnabled: [false], gamesChannelDiscordId: [''], autoPostPanel: [false] });
  rouletteForm = this.fb.group({ minPlayers: [2, [Validators.required, Validators.min(2), Validators.max(10)]], maxPlayers: [6, [Validators.required, Validators.min(2), Validators.max(10)]], winnerCoins: [100, [Validators.required, Validators.min(0), Validators.max(1000)]], secondPlaceCoins: [50, [Validators.required, Validators.min(0), Validators.max(500)]], participationCoins: [10, [Validators.required, Validators.min(0), Validators.max(100)]], joinWindowSeconds: [120, [Validators.required, Validators.min(30), Validators.max(300)]], turnSeconds: [30, [Validators.required, Validators.min(10), Validators.max(120)]], announceRoomCreated: [true], announceWinner: [true] });
  get stats(): PageWorkspaceHeroStat[] { return [{ label: 'الحالة', value: this.form.value.isEnabled ? 'مفعّلة' : 'غير مفعّلة' }, { label: 'الألعاب المفعّلة', value: String(this.games.filter(x => x.isEnabledForGuild).length) }, { label: 'اللاعبون', value: String(this.leaderboard.length) }]; }
  get action(): PageWorkspaceHeroAction { return { label: 'حفظ الإعدادات', disabled: this.saving }; }
  get soloGames(): GuildGame[] { return this.games.filter(game => game.playMode === 'Solo'); }
  get multiplayerGames(): GuildGame[] { return this.games.filter(game => game.playMode === 'Multiplayer'); }
  constructor(private route: ActivatedRoute, private fb: FormBuilder, private api: GuildService, private toast: ToastService) { }
  ngOnInit(): void { this.guildId = this.route.snapshot.paramMap.get('id') ?? ''; this.load(); }
  load(): void {
    this.loading = true; this.error = '';
    forkJoin({ settings: this.api.getGamesSettings(this.guildId), games: this.api.getGuildGames(this.guildId), channels: this.api.getChannels(this.guildId), leaderboard: this.api.getGamesLeaderboard(this.guildId), roulette: this.api.getRouletteSettings(this.guildId) }).subscribe({
      next: x => { this.form.patchValue({ isEnabled: x.settings.isEnabled, gamesChannelDiscordId: x.settings.gamesChannelDiscordId ?? '', autoPostPanel: x.settings.autoPostPanel }); this.rouletteForm.patchValue(x.roulette); this.games = x.games; this.channels = x.channels.filter(c => c.type === 0 || String(c.type).toLowerCase() === 'text'); this.leaderboard = x.leaderboard; this.loading = false; },
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
  saveRouletteSettings(): void {
    const x = this.rouletteForm.getRawValue(); if (this.rouletteForm.invalid || Number(x.maxPlayers) < Number(x.minPlayers)) { this.toast.error('راجع حدود إعدادات الروليت.'); return; }
    this.savingRoulette = true; this.api.updateRouletteSettings(this.guildId, { minPlayers: Number(x.minPlayers), maxPlayers: Number(x.maxPlayers), winnerCoins: Number(x.winnerCoins), secondPlaceCoins: Number(x.secondPlaceCoins), participationCoins: Number(x.participationCoins), joinWindowSeconds: Number(x.joinWindowSeconds), turnSeconds: Number(x.turnSeconds), announceRoomCreated: !!x.announceRoomCreated, announceWinner: !!x.announceWinner }).subscribe({ next: value => { this.savingRoulette = false; this.rouletteForm.patchValue(value); this.toast.success('تم حفظ إعدادات الروليت.'); }, error: e => { this.savingRoulette = false; this.toast.error(getApiErrorMessage(e, 'تعذر حفظ إعدادات الروليت.')); } });
  }
  private refreshGames(): void { this.api.getGuildGames(this.guildId).subscribe({ next: x => this.games = x }); }
}
