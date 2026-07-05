import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { ToastService } from '../../core/services/toast.service';
import { DiscordRole } from '../../core/models/guild.models';
import { UpdateGuildMusicSettings } from '../../core/models/music.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import { PageWorkspaceHeroAction, PageWorkspaceHeroStat } from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';

@Component({ selector: 'app-music-settings', templateUrl: './music-settings.component.html', styleUrls: ['../settings/settings.component.css', './music-settings.component.css'] })
export class MusicSettingsComponent implements OnInit {
  guildId = ''; roles: DiscordRole[] = []; loading = true; saving = false; error = '';
  form = this.fb.group({ isEnabled: [false], djRoleDiscordId: [''], maxQueueSize: [50, [Validators.required, Validators.min(1), Validators.max(200)]], maxTrackDurationSeconds: [600, [Validators.required, Validators.min(60), Validators.max(7200)]], defaultVolume: [50, [Validators.required, Validators.min(1), Validators.max(100)]], allowEveryoneToQueue: [true] });
  get stats(): PageWorkspaceHeroStat[] { return [{ label: this.t.instant('music.stats.status'), value: this.t.instant(this.form.value.isEnabled ? 'common.enabled' : 'common.disabled') }, { label: this.t.instant('music.stats.queue'), value: String(this.form.value.maxQueueSize ?? 50) }, { label: this.t.instant('music.stats.volume'), value: `${this.form.value.defaultVolume ?? 50}%` }]; }
  get action(): PageWorkspaceHeroAction { return { label: this.t.instant('common.save'), disabled: this.saving }; }
  constructor(private route: ActivatedRoute, private fb: FormBuilder, private api: GuildService, private toast: ToastService, private t: TranslateService) { }
  ngOnInit(): void { this.guildId = this.route.snapshot.paramMap.get('id') ?? ''; this.load(); }
  load(): void { this.loading = true; this.error = ''; forkJoin({ settings: this.api.getMusicSettings(this.guildId), roles: this.api.getRoles(this.guildId) }).subscribe({ next: x => { this.roles = x.roles.filter(r => !r.isManaged); this.form.patchValue({ ...x.settings, djRoleDiscordId: x.settings.djRoleDiscordId ?? '' }); this.loading = false; }, error: e => { this.loading = false; this.error = getApiErrorMessage(e, this.t.instant('music.messages.loadError')); } }); }
  save(): void { if (this.form.invalid) { this.form.markAllAsTouched(); this.toast.error(this.t.instant('music.messages.validation')); return; } this.saving = true; const raw = this.form.getRawValue(); const payload: UpdateGuildMusicSettings = { isEnabled: !!raw.isEnabled, djRoleDiscordId: raw.djRoleDiscordId || null, maxQueueSize: Number(raw.maxQueueSize), maxTrackDurationSeconds: Number(raw.maxTrackDurationSeconds), defaultVolume: Number(raw.defaultVolume), allowEveryoneToQueue: !!raw.allowEveryoneToQueue }; this.api.updateMusicSettings(this.guildId, payload).subscribe({ next: value => { this.saving = false; this.form.patchValue({ ...value, djRoleDiscordId: value.djRoleDiscordId ?? '' }); this.toast.success(this.t.instant('music.messages.saved')); }, error: e => { this.saving = false; this.toast.error(getApiErrorMessage(e, this.t.instant('music.messages.saveError'))); } }); }
}
