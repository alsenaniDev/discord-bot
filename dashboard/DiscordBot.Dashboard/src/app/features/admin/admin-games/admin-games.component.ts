import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { PlatformGameDefinition, SavePlatformGameDefinition } from '../../../core/models/games.models';
import { ToastService } from '../../../core/services/toast.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({ selector: 'app-admin-games', templateUrl: './admin-games.component.html', styleUrls: ['./admin-games.component.css'] })
export class AdminGamesComponent implements OnInit {
  games: PlatformGameDefinition[] = []; loading = true; saving = false; editingId: string | null = null; error = '';
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
}
