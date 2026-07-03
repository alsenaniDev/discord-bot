import { Injectable } from '@angular/core';
import { ActivationStep, GuildOverviewExperience } from '../models/guild.models';
import {
  CommunityPulseMapperInput,
  CommunityPulseModel,
  CommunityPulseMode,
  PulseMetricModel,
  PulseMetricTone
} from '../models/mission-control.models';
import { GuildModule } from '../models/module.models';
import { GuildAccess } from '../models/staff.models';

const TICKET_WARNING_THRESHOLD = 5;
const TICKET_DANGER_THRESHOLD = 10;
const HEALTH_WARNING_SCORE = 60;
const HEALTH_DANGER_SCORE = 40;

/**
 * Maps overview data to Community Pulse metrics until backend ships CommunityPulseDto.
 */
@Injectable({ providedIn: 'root' })
export class CommunityPulseMapperService {
  mapPulse(input: CommunityPulseMapperInput): CommunityPulseModel {
    const mode = this.resolveMode(input.experience.activation.steps);
    const metrics = mode === 'beginner'
      ? this.buildBeginnerMetrics(input)
      : this.buildVeteranMetrics(input);

    return {
      mode,
      loading: false,
      metrics
    };
  }

  createLoadingPulse(): CommunityPulseModel {
    return {
      mode: 'veteran',
      loading: true,
      metrics: this.buildPlaceholderMetrics(6)
    };
  }

  private resolveMode(steps: ActivationStep[]): CommunityPulseMode {
    const firstValueAchieved = steps.find(step => step.key === 'firstValue')?.completed ?? false;
    return firstValueAchieved ? 'veteran' : 'beginner';
  }

  private buildBeginnerMetrics(input: CommunityPulseMapperInput): PulseMetricModel[] {
    const metrics: PulseMetricModel[] = [
      this.setupMetric(input.experience.activation.steps),
      this.healthMetric(input.experience.health.score, input.experience.health.level),
      this.botMetric(input.experience.botOnline)
    ];

    if (this.canShowModules(input.access)) {
      metrics.push(this.modulesMetric(input.modules));
    }

    return metrics;
  }

  private buildVeteranMetrics(input: CommunityPulseMapperInput): PulseMetricModel[] {
    const metrics: PulseMetricModel[] = [
      this.healthMetric(input.experience.health.score, input.experience.health.level),
      this.communitySizeMetric(input.overview.totalChannels, input.overview.totalRoles),
      this.openTicketsMetric(input.overview.openTickets),
      this.logsMetric(input.experience),
      this.botMetric(input.experience.botOnline)
    ];

    if (this.canShowModules(input.access)) {
      metrics.push(this.modulesMetric(input.modules));
    }

    return metrics;
  }

  private buildPlaceholderMetrics(count: number): PulseMetricModel[] {
    return Array.from({ length: count }, (_, index) => ({
      id: `placeholder-${index}`,
      labelKey: 'overview.v2.pulse.loading',
      valueKey: 'common.emptyValue',
      tone: 'muted' as PulseMetricTone
    }));
  }

  private setupMetric(steps: ActivationStep[]): PulseMetricModel {
    const connectDone = this.phaseComplete(steps, ['addBot', 'linkGuild']);
    const configureDone = this.phaseComplete(steps, ['enableModule', 'configureModule']);

    let phaseKey = 'overview.v2.pulse.setupPhase.connect';
    if (connectDone && !configureDone) {
      phaseKey = 'overview.v2.pulse.setupPhase.configure';
    } else if (connectDone && configureDone) {
      phaseKey = 'overview.v2.pulse.setupPhase.firstWin';
    }

    return {
      id: 'setup',
      labelKey: 'overview.v2.pulse.setup',
      valueKey: phaseKey,
      tone: 'default'
    };
  }

  private healthMetric(score: number, level: string): PulseMetricModel {
    return {
      id: 'health',
      labelKey: 'overview.v2.pulse.health',
      health: {
        score,
        levelLabelKey: this.healthLevelLabelKey(level)
      },
      tone: this.healthTone(score)
    };
  }

  private communitySizeMetric(totalChannels: number, totalRoles: number): PulseMetricModel {
    if (totalChannels > 0) {
      return {
        id: 'channels',
        labelKey: 'overview.v2.pulse.channels',
        valueKey: 'overview.v2.pulse.countValue',
        valueParams: { count: totalChannels },
        tone: 'default'
      };
    }

    if (totalRoles > 0) {
      return {
        id: 'roles',
        labelKey: 'overview.v2.pulse.roles',
        valueKey: 'overview.v2.pulse.countValue',
        valueParams: { count: totalRoles },
        tone: 'default'
      };
    }

    return {
      id: 'channels',
      labelKey: 'overview.v2.pulse.channels',
      valueKey: 'common.emptyValue',
      tone: 'muted'
    };
  }

  private openTicketsMetric(openTickets: number): PulseMetricModel {
    return {
      id: 'openTickets',
      labelKey: 'overview.v2.pulse.openTickets',
      valueKey: 'overview.v2.pulse.countValue',
      valueParams: { count: openTickets },
      tone: this.ticketTone(openTickets)
    };
  }

  private logsMetric(experience: GuildOverviewExperience): PulseMetricModel {
    const logEntries = experience.recentActivity.filter(item => item.type === 'LogEntry').length;

    if (logEntries > 0) {
      return {
        id: 'logsToday',
        labelKey: 'overview.v2.pulse.logsToday',
        valueKey: 'overview.v2.pulse.countValue',
        valueParams: { count: logEntries },
        tone: 'default'
      };
    }

    const activityCount = experience.recentActivity.length;
    if (activityCount > 0) {
      return {
        id: 'recentActivity',
        labelKey: 'overview.v2.pulse.recentActivity',
        valueKey: 'overview.v2.pulse.countValue',
        valueParams: { count: activityCount },
        tone: 'muted'
      };
    }

    return {
      id: 'logsToday',
      labelKey: 'overview.v2.pulse.logsToday',
      valueKey: 'common.emptyValue',
      tone: 'muted'
    };
  }

  private botMetric(botOnline: boolean): PulseMetricModel {
    return {
      id: 'bot',
      labelKey: 'overview.v2.pulse.bot',
      valueKey: botOnline ? 'overview.v2.status.botOnline' : 'overview.v2.status.botOffline',
      tone: botOnline ? 'success' : 'warning'
    };
  }

  private modulesMetric(modules: GuildModule[]): PulseMetricModel {
    const total = modules.length;
    const enabled = modules.filter(module => module.effectiveEnabled ?? (module.isEnabled && module.allowedByPlan)).length;

    return {
      id: 'modules',
      labelKey: 'overview.v2.pulse.modules',
      valueKey: 'overview.v2.pulse.modulesActive',
      valueParams: { enabled, total },
      tone: enabled > 0 ? 'default' : 'muted'
    };
  }

  private canShowModules(access: GuildAccess): boolean {
    return access.canManageModules || access.canManageSettings;
  }

  private phaseComplete(steps: ActivationStep[], keys: string[]): boolean {
    return keys.every(key => steps.find(step => step.key === key)?.completed ?? false);
  }

  private healthLevelLabelKey(level: string): string {
    return `overview.health.level.${level.charAt(0).toLowerCase()}${level.slice(1)}`;
  }

  private healthTone(score: number): PulseMetricTone {
    if (score < HEALTH_DANGER_SCORE) {
      return 'danger';
    }

    if (score < HEALTH_WARNING_SCORE) {
      return 'warning';
    }

    return 'default';
  }

  private ticketTone(openTickets: number): PulseMetricTone {
    if (openTickets >= TICKET_DANGER_THRESHOLD) {
      return 'danger';
    }

    if (openTickets >= TICKET_WARNING_THRESHOLD) {
      return 'warning';
    }

    return 'default';
  }
}
