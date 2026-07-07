import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { GameVersion, PlatformGameDefinition, SavePlatformGameDefinition } from '../../../core/models/games.models';
import { ToastService } from '../../../core/services/toast.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({ selector: 'app-admin-games', templateUrl: './admin-games.component.html', styleUrls: ['./admin-games.component.css'] })
export class AdminGamesComponent implements OnInit {
  games: PlatformGameDefinition[] = []; loading = true; saving = false; editingId: string | null = null; error = '';
  versions: Record<string, GameVersion[]> = {}; loadingVersions: Record<string, boolean> = {};
  form = this.fb.group({
    key: ['', [Validators.required, Validators.pattern(/^[a-z0-9][a-z0-9-]{0,63}$/)]], name: ['', Validators.required], description: [''], iconUrl: [''], activityRoute: ['/games/', Validators.required], requiredPlan: ['free', Validators.required], playMode: ['Solo' as 'Solo' | 'Multiplayer', Validators.required],
    isEnabledGlobally: [true], defaultPointsPerWin: [10, [Validators.required, Validators.min(0)]], defaultCooldownSeconds: [30, [Validators.required, Validators.min(0)]], defaultMaxPlaysPerDay: [10, [Validators.required, Validators.min(0)]],
    supportsScores: [true], supportsLeaderboard: [true], supportsResultPublishing: [true]
  });
  constructor(private fb: FormBuilder, private api: AdminService, private toast: ToastService) { }
  ngOnInit(): void { this.load(); }
  load(): void { this.loading = true; this.api.getGames().subscribe({ next: x => { this.games = x; this.loading = false; }, error: e => { this.loading = false; this.error = getApiErrorMessage(e, 'تعذر تحميل كتالوج الألعاب.'); } }); }
  edit(game: PlatformGameDefinition): void { this.editingId = game.id; this.form.patchValue({ ...game, description: game.description ?? '', iconUrl: game.iconUrl ?? '' }); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  cancel(): void { this.editingId = null; this.form.reset({ key: '', name: '', description: '', iconUrl: '', activityRoute: '/games/', requiredPlan: 'free', playMode: 'Solo', isEnabledGlobally: true, defaultPointsPerWin: 10, defaultCooldownSeconds: 30, defaultMaxPlaysPerDay: 10, supportsScores: true, supportsLeaderboard: true, supportsResultPublishing: true }); }
  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); this.toast.error('راجع الحقول المطلوبة والقيم المدخلة.'); return; }
    const x = this.form.getRawValue(); const value: SavePlatformGameDefinition = { key: x.key!, name: x.name!, description: x.description || null, iconUrl: x.iconUrl || null, activityRoute: x.activityRoute!, requiredPlan: x.requiredPlan!, playMode: x.playMode!, isEnabledGlobally: !!x.isEnabledGlobally, defaultPointsPerWin: Number(x.defaultPointsPerWin), defaultCooldownSeconds: Number(x.defaultCooldownSeconds), defaultMaxPlaysPerDay: Number(x.defaultMaxPlaysPerDay), supportsScores: !!x.supportsScores, supportsLeaderboard: !!x.supportsLeaderboard, supportsResultPublishing: !!x.supportsResultPublishing };
    this.saving = true; const request = this.editingId ? this.api.updateGame(this.editingId, value) : this.api.createGame(value);
    request.subscribe({ next: () => { this.saving = false; this.toast.success(this.editingId ? 'تم تحديث اللعبة.' : 'تمت إضافة اللعبة.'); this.cancel(); this.load(); }, error: e => { this.saving = false; this.toast.error(getApiErrorMessage(e, 'تعذر حفظ اللعبة.')); } });
  }
  toggle(game: PlatformGameDefinition): void { this.api.toggleGame(game.id).subscribe({ next: x => { Object.assign(game, x); this.toast.success(x.isEnabledGlobally ? 'تم تفعيل اللعبة عالميًا.' : 'تم إيقاف اللعبة عالميًا.'); }, error: e => this.toast.error(getApiErrorMessage(e, 'تعذر تغيير حالة اللعبة.')) }); }
  statusLabel(status: string): string { return ({ Draft: 'مسودة', Sandbox: 'تجريبية', InReview: 'قيد المراجعة', Published: 'منشورة', Rejected: 'مرفوضة', Disabled: 'معطلة' } as Record<string, string>)[status] ?? status; }
  loadVersions(game: PlatformGameDefinition): void {
    this.loadingVersions[game.id] = true;
    this.api.getGameVersions(game.id).subscribe({ next: x => { this.versions[game.id] = x; this.loadingVersions[game.id] = false; }, error: e => { this.loadingVersions[game.id] = false; this.toast.error(getApiErrorMessage(e, 'تعذر تحميل إصدارات اللعبة.')); } });
  }
  createSandboxVersion(game: PlatformGameDefinition): void {
    const version = window.prompt('رقم الإصدار التجريبي', '1.0.1');
    if (!version) return;
    const manifest = { key: game.key, name: game.name, description: game.description, playMode: game.playMode, engineType: 'Hybrid', frontendMode: 'InternalRoute', activityRoute: game.activityRoute, requiredPlan: game.requiredPlan, supportsWallet: game.key === 'roulette', supportsLeaderboard: game.supportsLeaderboard, supportsPowerUps: game.key === 'roulette', supportsBotPublishing: game.supportsResultPublishing, events: [`${game.key}.sandbox`], permissions: [], sandboxAllowedOrigins: [], configSchema: {} };
    this.api.createGameVersion(game.id, { version, status: 'Sandbox', activityRoute: game.activityRoute, manifestJson: JSON.stringify(manifest), notes: 'نسخة تجريبية من لوحة الإدارة.' }).subscribe({ next: () => { this.toast.success('تم إنشاء إصدار تجريبي.'); this.loadVersions(game); }, error: e => this.toast.error(getApiErrorMessage(e, 'تعذر إنشاء الإصدار التجريبي.')) });
  }
  updateVersionStatus(game: PlatformGameDefinition, version: GameVersion, status: string): void {
    this.api.updateGameVersionStatus(version.id, status).subscribe({ next: () => { this.toast.success('تم تحديث حالة الإصدار.'); this.loadVersions(game); }, error: e => this.toast.error(getApiErrorMessage(e, 'تعذر تحديث حالة الإصدار.')) });
  }
  addSandboxGuild(game: PlatformGameDefinition, version: GameVersion): void {
    const guildDiscordId = window.prompt('Discord ID لسيرفر الاختبار');
    if (!guildDiscordId) return;
    const userDiscordId = window.prompt('Discord ID لمستخدم محدد (اختياري)') || null;
    this.api.addGameSandboxAccess(version.id, guildDiscordId, userDiscordId).subscribe({ next: () => { this.toast.success('تمت إضافة سيرفر الاختبار.'); this.loadVersions(game); }, error: e => this.toast.error(getApiErrorMessage(e, 'تعذر إضافة صلاحية الاختبار.')) });
  }
  removeSandboxAccess(game: PlatformGameDefinition, accessId: string): void {
    this.api.removeGameSandboxAccess(accessId).subscribe({ next: () => { this.toast.success('تم حذف صلاحية الاختبار.'); this.loadVersions(game); }, error: e => this.toast.error(getApiErrorMessage(e, 'تعذر حذف صلاحية الاختبار.')) });
  }
}
