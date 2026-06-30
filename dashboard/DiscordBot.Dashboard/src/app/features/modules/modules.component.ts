import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { GuildModule } from '../../core/models/module.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-modules',
  templateUrl: './modules.component.html',
  styleUrls: ['./modules.component.css']
})
export class ModulesComponent implements OnInit {
  guildId = '';
  modules: GuildModule[] = [];
  loading = true;
  error = '';
  savingKey: string | null = null;

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
    this.loadModules();
  }

  loadModules(): void {
    this.loading = true;
    this.error = '';

    this.guildService.getModules(this.guildId).subscribe({
      next: modules => {
        this.modules = modules;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('errors.loadModules'));
        this.loading = false;
      }
    });
  }

  effectiveEnabled(module: GuildModule): boolean {
    return module.effectiveEnabled ?? (module.isEnabled && module.allowedByPlan);
  }

  onToggle(module: GuildModule, enabled: boolean): void {
    if (this.savingKey || !module.allowedByPlan) {
      return;
    }

    const previous = { ...module };
    module.isEnabled = enabled;
    module.effectiveEnabled = enabled && module.allowedByPlan;
    this.savingKey = module.key;

    this.guildService.updateModule(this.guildId, module.key, { isEnabled: enabled }).subscribe({
      next: updated => {
        module.isEnabled = updated.isEnabled;
        module.allowedByPlan = updated.allowedByPlan;
        module.effectiveEnabled = updated.effectiveEnabled;
        this.savingKey = null;
        this.toast.success(
          this.translate.instant(
            this.effectiveEnabled(updated) ? 'modules.moduleEnabled' : 'modules.moduleDisabled',
            { name: updated.name }
          )
        );
      },
      error: err => {
        module.isEnabled = previous.isEnabled;
        module.allowedByPlan = previous.allowedByPlan;
        module.effectiveEnabled = previous.effectiveEnabled;
        this.savingKey = null;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('modules.updateError')));
      }
    });
  }

  isSaving(module: GuildModule): boolean {
    return this.savingKey === module.key;
  }

  canToggle(module: GuildModule): boolean {
    return !this.isSaving(module) && module.allowedByPlan;
  }
}
