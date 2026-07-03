import { Injectable } from '@angular/core';
import {
  ContextDrawerHelpLink,
  ContextDrawerMapperInput,
  ContextDrawerModel,
  ContextDrawerModuleRow,
  ContextDrawerModuleStatus,
  ContextDrawerSetupModel,
  ContextDrawerSubscriptionModel,
  ContextDrawerSuggestionRow
} from '../models/mission-control.models';

const MODULE_NAME_KEYS: Record<string, string> = {
  welcome: 'overview.v2.drawer.moduleNames.welcome',
  logs: 'overview.v2.drawer.moduleNames.logs',
  'reaction-roles': 'overview.v2.drawer.moduleNames.reactionRoles',
  tickets: 'overview.v2.drawer.moduleNames.tickets',
  moderation: 'overview.v2.drawer.moduleNames.moderation',
  'auto-role': 'overview.v2.drawer.moduleNames.autoRole'
};

const HELP_LINKS: ContextDrawerHelpLink[] = [
  {
    id: 'docs',
    labelKey: 'overview.v2.drawer.help.docs',
    url: 'https://github.com',
    external: true
  },
  {
    id: 'support',
    labelKey: 'overview.v2.drawer.help.support',
    url: 'https://discord.com',
    external: true
  },
  {
    id: 'status',
    labelKey: 'overview.v2.drawer.help.status',
    url: 'https://discordstatus.com',
    external: true
  },
  {
    id: 'releaseNotes',
    labelKey: 'overview.v2.drawer.help.releaseNotes',
    url: 'https://github.com',
    external: true
  }
];

/**
 * Maps overview data into Context Drawer sections.
 * Temporary until backend ships a dedicated drawer DTO.
 */
@Injectable({ providedIn: 'root' })
export class ContextDrawerMapperService {
  mapDrawer(input: ContextDrawerMapperInput): ContextDrawerModel {
    const showSubscription = input.access.canManageSubscription;
    const showSetup = !input.experience.activation.isActivated;
    const subscription = showSubscription ? this.mapSubscription(input) : null;
    const setup = showSetup ? this.mapSetup(input) : null;
    const suggestions = this.mapSuggestions(input);
    const modules = this.mapModules(input);

    const sectionHints: string[] = [];
    if (modules.length > 0) {
      sectionHints.push('overview.v2.drawer.sections.modules');
    }
    if (showSubscription) {
      sectionHints.push('overview.v2.drawer.sections.subscription');
    }
    if (showSetup) {
      sectionHints.push('overview.v2.drawer.sections.setup');
    }
    sectionHints.push('overview.v2.drawer.sections.help');
    if (suggestions.length > 0) {
      sectionHints.push('overview.v2.drawer.sections.suggestions');
    }

    return {
      loading: false,
      modules,
      showSubscription,
      subscription,
      showSetup,
      setup,
      helpLinks: HELP_LINKS,
      suggestions,
      sectionHints
    };
  }

  createLoadingDrawer(): ContextDrawerModel {
    return {
      loading: true,
      modules: [],
      showSubscription: false,
      subscription: null,
      showSetup: false,
      setup: null,
      helpLinks: [],
      suggestions: [],
      sectionHints: []
    };
  }

  private mapModules(input: ContextDrawerMapperInput): ContextDrawerModuleRow[] {
    return input.modules.map(module => ({
      key: module.key,
      nameKey: MODULE_NAME_KEYS[module.key] ?? 'overview.v2.drawer.moduleNames.unknown',
      status: this.resolveModuleStatus(module),
      route: `/guilds/${input.guildId}/modules`
    }));
  }

  private resolveModuleStatus(module: ContextDrawerMapperInput['modules'][number]): ContextDrawerModuleStatus {
    if (module.effectiveEnabled ?? (module.isEnabled && module.allowedByPlan)) {
      return 'enabled';
    }

    if (module.isEnabled && !module.allowedByPlan) {
      return 'warning';
    }

    return 'disabled';
  }

  private mapSubscription(input: ContextDrawerMapperInput): ContextDrawerSubscriptionModel {
    const subscription = input.experience.subscription;
    const expiresAt = subscription.expiresAt ?? null;
    const showRenew =
      subscription.isExpired
      || (expiresAt !== null && this.daysUntil(expiresAt) <= 7);

    return {
      planName: subscription.planName || subscription.planKey,
      expiresAt,
      showRenew,
      renewRoute: `/guilds/${input.guildId}/subscription`,
      manageRoute: `/guilds/${input.guildId}/subscription`
    };
  }

  private mapSetup(input: ContextDrawerMapperInput): ContextDrawerSetupModel {
    const activation = input.experience.activation;
    const remainingSteps = activation.steps.filter(step => !step.completed).length;
    const steps = activation.steps;
    const connectDone = this.phaseComplete(steps, ['addBot', 'linkGuild']);
    const configureDone = this.phaseComplete(steps, ['enableModule', 'configureModule']);

    let phaseLabelKey = 'overview.v2.pulse.setupPhase.connect';
    if (connectDone && !configureDone) {
      phaseLabelKey = 'overview.v2.pulse.setupPhase.configure';
    } else if (connectDone && configureDone) {
      phaseLabelKey = 'overview.v2.pulse.setupPhase.firstWin';
    }

    return {
      phaseLabelKey,
      remainingSteps,
      progressPercent: activation.progressPercent,
      resumeRoute: `/guilds/${input.guildId}/${activation.primaryActionRoute}`
    };
  }

  private phaseComplete(steps: ContextDrawerMapperInput['experience']['activation']['steps'], keys: string[]): boolean {
    return keys.every(key => steps.some(step => step.key === key && step.completed));
  }

  private mapSuggestions(input: ContextDrawerMapperInput): ContextDrawerSuggestionRow[] {
    return input.experience.recommendations
      .filter(item => item.priority !== 'High')
      .slice(0, 2)
      .map(item => ({
        id: item.id,
        titleKey: `overview.recommendations.${item.id}.title`,
        route: `/guilds/${input.guildId}/${item.route}`
      }));
  }

  private daysUntil(isoDate: string): number {
    const expires = new Date(isoDate);
    if (Number.isNaN(expires.getTime())) {
      return Number.MAX_SAFE_INTEGER;
    }

    return Math.ceil((expires.getTime() - Date.now()) / (24 * 60 * 60 * 1000));
  }
}
