import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { GuildModule } from '../../core/models/module.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import {
  getModuleUiMeta,
  MODULE_CATEGORY_ORDER,
  ModuleCategory
} from './config/module-workspace.config';
import {
  PageWorkspaceHeroAction,
  PageWorkspaceHeroStat
} from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';
import {
  ModuleCardAction,
  ModuleCardStatus
} from './modules-module-card/modules-module-card.component';

type ModulesHeroCta = 'upgrade' | 'review' | 'none';

interface ModuleCategoryGroup {
  category: ModuleCategory;
  modules: GuildModule[];
}

@Component({
  selector: 'app-modules',
  templateUrl: './modules.component.html',
  styleUrls: ['./modules.component.css']
})
export class ModulesComponent implements OnInit {
  guildId = '';
  modules: GuildModule[] = [];
  planName = '';
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
        this.loadPlanName();
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('modules.loadError'));
        this.loading = false;
      }
    });
  }

  private loadPlanName(): void {
    this.guildService.getSubscriptionStatus(this.guildId).subscribe({
      next: status => {
        this.planName = status.subscription?.planName ?? '';
      },
      error: () => {
        this.planName = '';
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
            { name: this.moduleDisplayName(updated) }
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

  get activeCount(): number {
    return this.modules.filter(module => this.effectiveEnabled(module)).length;
  }

  get availableCount(): number {
    return this.modules.filter(module => module.allowedByPlan).length;
  }

  get lockedCount(): number {
    return this.modules.filter(module => !module.allowedByPlan).length;
  }

  get allModulesAvailable(): boolean {
    return this.modules.length > 0 && this.lockedCount === 0;
  }

  get heroCta(): ModulesHeroCta {
    if (this.lockedCount > 0) {
      return 'upgrade';
    }

    if (this.modules.some(module => module.allowedByPlan && !this.effectiveEnabled(module))) {
      return 'review';
    }

    return 'none';
  }

  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    return [
      {
        label: this.translate.instant('modules.hero.stats.active'),
        value: String(this.activeCount)
      },
      {
        label: this.translate.instant('modules.hero.stats.locked'),
        value: String(this.lockedCount)
      },
      {
        label: this.translate.instant('modules.hero.stats.available'),
        value: String(this.availableCount)
      },
      {
        label: this.translate.instant('modules.hero.currentPlan'),
        value: this.planName || this.translate.instant('modules.hero.planUnknown'),
        compact: true
      }
    ];
  }

  get workspaceHeroFooter(): string {
    if (this.allModulesAvailable) {
      return this.translate.instant('modules.hero.footer.allAvailable', {
        plan: this.planName || this.translate.instant('modules.hero.planUnknown')
      });
    }

    return this.translate.instant('modules.hero.footer.upgradeRequired', {
      count: this.lockedCount
    });
  }

  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction | null {
    if (this.heroCta === 'none') {
      return null;
    }

    const labelKey = this.heroCta === 'review'
      ? 'modules.hero.cta.review'
      : 'modules.hero.cta.upgrade';

    return {
      label: this.translate.instant(labelKey)
    };
  }

  get groupedModules(): ModuleCategoryGroup[] {
    return MODULE_CATEGORY_ORDER
      .map(category => ({
        category,
        modules: this.modules.filter(module => getModuleUiMeta(module.key)?.category === category)
      }))
      .filter(group => group.modules.length > 0);
  }

  moduleIcon(key: string): string {
    return getModuleUiMeta(key)?.icon ?? 'overview';
  }

  moduleIconTone(key: string): string {
    return getModuleUiMeta(key)?.iconTone ?? 'blue';
  }

  moduleDisplayName(module: GuildModule): string {
    const key = `modules.moduleNames.${module.key}.name`;
    const translated = this.translate.instant(key);
    return translated === key ? module.name : translated;
  }

  moduleDisplayDescription(module: GuildModule): string {
    const key = `modules.moduleNames.${module.key}.description`;
    const translated = this.translate.instant(key);
    return translated === key ? module.description : translated;
  }

  moduleStatus(module: GuildModule): ModuleCardStatus {
    if (!module.allowedByPlan) {
      return 'locked';
    }

    if (this.effectiveEnabled(module)) {
      return 'enabled';
    }

    return 'disabled';
  }

  modulePrimaryAction(module: GuildModule): ModuleCardAction {
    if (!module.allowedByPlan) {
      return 'upgrade';
    }

    if (this.effectiveEnabled(module)) {
      return 'configure';
    }

    return 'enable';
  }

  onModulePrimaryAction(module: GuildModule): void {
    const action = this.modulePrimaryAction(module);

    switch (action) {
      case 'upgrade':
        this.router.navigate(['/guilds', this.guildId, 'subscription']);
        break;
      case 'enable':
        this.onToggle(module, true);
        break;
      case 'configure':
        this.navigateToModule(module);
        break;
    }
  }

  onHeroCta(action: ModulesHeroCta): void {
    this.router.navigate(['/guilds', this.guildId, 'subscription']);
  }

  categoryTitleKey(category: ModuleCategory): string {
    return `modules.categories.${category}.title`;
  }

  categoryLeadKey(category: ModuleCategory): string {
    return `modules.categories.${category}.lead`;
  }

  private navigateToModule(module: GuildModule): void {
    const route = getModuleUiMeta(module.key)?.route ?? ['settings'];
    this.router.navigate(['/guilds', this.guildId, ...route]);
  }
}
