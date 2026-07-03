import { Injectable } from '@angular/core';
import {
  ActivationStep,
  GuildOverviewExperience,
  OverviewRecommendation
} from '../models/guild.models';
import {
  BeginnerProgressPhase,
  MissionCardModel,
  MissionIconName,
  MissionId,
  MissionMapperInput,
  MissionSeverity,
  StatusStripModel
} from '../models/mission-control.models';
import { GuildAccess } from '../models/staff.models';
import { MissionDismissService } from './mission-dismiss.service';

const SYNC_STALE_MS = 7 * 24 * 60 * 60 * 1000;
const SUBSCRIPTION_EXPIRING_DAYS = 7;
const TICKET_CRITICAL_THRESHOLD = 10;
const TICKET_ELEVATED_THRESHOLD = 5;

interface RankedMission {
  rank: number;
  mission: MissionCardModel;
}

/**
 * Temporary mapper until PX-002 Mission Engine ships on the backend.
 * Remove this service when overview API returns a single Mission DTO.
 */
@Injectable({ providedIn: 'root' })
export class MissionMapperService {
  constructor(private dismissService: MissionDismissService) {}

  mapStatusStrip(
    experience: GuildOverviewExperience,
    access: GuildAccess,
    syncing: boolean,
    resourcesSyncedAt: string | null | undefined
  ): StatusStripModel {
    return {
      showPlan: access.canManageSubscription,
      planName: experience.subscription.planName,
      showStaffAccess: !access.canManageSubscription,
      botOnline: experience.botOnline,
      syncing,
      resourcesSyncedAt
    };
  }

  mapMissionCard(input: MissionMapperInput, userId?: string): MissionCardModel {
    const candidates = this.buildCandidates(input);
    const eligible = candidates.filter(
      candidate => !this.dismissService.isSnoozed(candidate.mission.missionId, input.guildId, userId)
    );

    const winner = eligible.sort((a, b) => a.rank - b.rank)[0]?.mission
      ?? this.createMission('EverythingOperational', 'neutral', 'check-circle', input);

    return {
      ...winner,
      loading: false,
      error: false
    };
  }

  createLoadingMission(): MissionCardModel {
    return {
      missionId: 'EverythingOperational',
      severity: 'neutral',
      icon: 'check-circle',
      titleKey: 'overview.v2.mission.loading.title',
      descriptionKey: 'overview.v2.mission.loading.body',
      dismissible: false,
      showProgress: false,
      loading: true,
      error: false
    };
  }

  createErrorMission(): MissionCardModel {
    return {
      missionId: 'EverythingOperational',
      severity: 'neutral',
      icon: 'alert-circle',
      titleKey: 'overview.v2.mission.error.title',
      descriptionKey: 'overview.v2.mission.error.body',
      dismissible: false,
      showProgress: false,
      loading: false,
      error: true
    };
  }

  private buildCandidates(input: MissionMapperInput): RankedMission[] {
    const { overview, experience, access } = input;
    const candidates: RankedMission[] = [];
    const subscription = experience.subscription;
    const firstValueAchieved = this.isFirstValueAchieved(experience.activation.steps);
    const isBeginner = !firstValueAchieved;

    const add = (rank: number, mission: MissionCardModel) => {
      if (this.isMissionAllowed(mission.missionId, access)) {
        candidates.push({ rank, mission });
      }
    };

    if (!experience.botOnline) {
      add(100, this.createMission('BotOffline', 'critical', 'bot', input));
    }

    if (access.canManageSubscription && subscription.isExpired) {
      add(110, this.createMission('SubscriptionExpired', 'critical', 'subscription', input));
    }

    if (this.isSyncStale(overview.resourcesSyncedAt) && experience.botOnline) {
      add(200, this.createMission('SynchronizationStale', 'warning', 'refresh', input, { dismissible: true }));
    }

    if (overview.openTickets >= TICKET_CRITICAL_THRESHOLD) {
      add(210, this.createMission('TicketBacklogCritical', 'warning', 'tickets', input, {
        descriptionParams: { count: overview.openTickets }
      }));
    } else if (overview.openTickets >= TICKET_ELEVATED_THRESHOLD) {
      add(300, this.createMission('TicketBacklogElevated', 'warning', 'tickets', input, {
        descriptionParams: { count: overview.openTickets },
        dismissible: true
      }));
    }

    if (access.canManageSubscription && this.isSubscriptionExpiringSoon(subscription.expiresAt, subscription.isExpired)) {
      const days = this.daysUntil(subscription.expiresAt!);
      add(301, this.createMission('SubscriptionExpiringSoon', 'warning', 'clock', input, {
        descriptionParams: { days },
        dismissible: true
      }));
    }

    if (isBeginner && access.canManageSettings) {
      const setupMission = this.buildSetupMission(experience.activation.steps, input);
      if (setupMission) {
        add(400, setupMission);
      }
    }

    if (!overview.resourcesSyncedAt && experience.botOnline) {
      add(401, this.createMission('SynchronizationNever', 'info', 'cloud-off', input));
    }

    const topRecommendation = experience.recommendations[0];
    if (topRecommendation && (!isBeginner || !access.canManageSettings)) {
      const recommendationMission = this.recommendationToMission(topRecommendation, input, firstValueAchieved);
      if (recommendationMission) {
        add(500, recommendationMission);
      }
    } else if (topRecommendation && isBeginner) {
      const recommendationMission = this.recommendationToMission(topRecommendation, input, firstValueAchieved);
      if (recommendationMission) {
        add(501, recommendationMission);
      }
    }

    if (candidates.length === 0) {
      if (!access.canManageSubscription && !access.canManageSettings) {
        add(600, this.createMission('StaffCalm', 'neutral', 'check-circle', input));
      } else {
        add(600, this.createMission('EverythingOperational', 'neutral', 'check-circle', input, {
          descriptionParams: { score: experience.health.score }
        }));
      }
    }

    return candidates;
  }

  private buildSetupMission(steps: ActivationStep[], input: MissionMapperInput): MissionCardModel | null {
    const connectDone = this.phaseComplete(steps, ['addBot', 'linkGuild']);
    const configureDone = this.phaseComplete(steps, ['enableModule', 'configureModule']);
    const firstWinDone = steps.find(step => step.key === 'firstValue')?.completed ?? false;

    const progressPhases = this.buildProgressPhases(connectDone, configureDone, firstWinDone);
    const progressPercent = input.experience.activation.progressPercent;

    if (!connectDone) {
      return {
        ...this.createMission('CompleteSetupConnect', 'info', 'external', input),
        showProgress: true,
        progressPhases,
        progressPercent
      };
    }

    if (!configureDone) {
      return {
        ...this.createMission('CompleteSetupConfigure', 'info', 'settings', input),
        showProgress: true,
        progressPhases,
        progressPercent
      };
    }

    if (!firstWinDone) {
      const firstWinStep = steps.find(step => step.key === 'firstValue');
      return {
        ...this.createMission('CompleteSetupFirstValue', 'info', 'check-circle', input, {
          ctaRoute: firstWinStep?.actionRoute ?? 'tickets'
        }),
        showProgress: true,
        progressPhases,
        progressPercent
      };
    }

    return null;
  }

  private buildProgressPhases(
    connectDone: boolean,
    configureDone: boolean,
    firstWinDone: boolean
  ): BeginnerProgressPhase[] {
    const currentPhase: BeginnerProgressPhase['key'] = !connectDone
      ? 'connect'
      : !configureDone
        ? 'configure'
        : 'firstWin';

    const phaseKeys: BeginnerProgressPhase['key'][] = ['connect', 'configure', 'firstWin'];
    const labelKeys: Record<BeginnerProgressPhase['key'], string> = {
      connect: 'overview.v2.progress.connect',
      configure: 'overview.v2.progress.configure',
      firstWin: 'overview.v2.progress.firstWin'
    };

    return phaseKeys.map(key => {
      let status: BeginnerProgressPhase['status'] = 'upcoming';
      if (key === 'connect' && connectDone) {
        status = 'completed';
      } else if (key === 'configure' && configureDone) {
        status = 'completed';
      } else if (key === 'firstWin' && firstWinDone) {
        status = 'completed';
      } else if (key === currentPhase) {
        status = 'current';
      }

      return {
        key,
        labelKey: labelKeys[key],
        status
      };
    });
  }

  private recommendationToMission(
    recommendation: OverviewRecommendation,
    input: MissionMapperInput,
    firstValueAchieved: boolean
  ): MissionCardModel | null {
    const mapping: Record<string, MissionId> = {
      syncResources: 'SynchronizationNever',
      enableModules: 'EnableModule',
      configureWelcome: 'CreateWelcome',
      createTicketPanel: 'CreateTicketPanel',
      openFirstTicket: 'CompleteSetupFirstValue',
      enableLogs: 'ReviewLogs',
      inviteStaff: 'InviteStaff',
      createReactionPanel: 'CreateReactionPanel',
      renewSubscription: 'SubscriptionExpiringSoon',
      upgradeSubscription: 'PaymentRequired'
    };

    const missionId = mapping[recommendation.id];
    if (!missionId) {
      return null;
    }

    if (firstValueAchieved && missionId.startsWith('CompleteSetup')) {
      return null;
    }

    const iconMap: Partial<Record<MissionId, MissionIconName>> = {
      SynchronizationNever: 'cloud-off',
      EnableModule: 'modules',
      CreateWelcome: 'settings',
      CreateTicketPanel: 'tickets',
      CompleteSetupFirstValue: 'check-circle',
      ReviewLogs: 'logs',
      InviteStaff: 'users',
      CreateReactionPanel: 'roles',
      SubscriptionExpiringSoon: 'clock',
      PaymentRequired: 'subscription'
    };

    return this.createMission(
      missionId,
      'info',
      iconMap[missionId] ?? 'settings',
      input,
      {
        ctaRoute: recommendation.route,
        dismissible: true,
        descriptionParams:
          missionId === 'SubscriptionExpiringSoon' && input.experience.subscription.expiresAt
            ? { days: this.daysUntil(input.experience.subscription.expiresAt) }
            : undefined
      }
    );
  }

  private createMission(
    missionId: MissionId,
    severity: MissionSeverity,
    icon: MissionIconName,
    input: MissionMapperInput,
    options: {
      descriptionParams?: Record<string, string | number>;
      ctaRoute?: string;
      dismissible?: boolean;
    } = {}
  ): MissionCardModel {
    const ctaConfig = this.ctaForMission(missionId, input, options.ctaRoute);
    const dismissible = options.dismissible ?? this.defaultDismissible(missionId);

    return {
      missionId,
      severity,
      icon,
      titleKey: `overview.v2.mission.${this.missionKey(missionId)}.title`,
      descriptionKey: `overview.v2.mission.${this.missionKey(missionId)}.body`,
      descriptionParams: options.descriptionParams,
      cta: ctaConfig,
      dismissible,
      showProgress: false,
      loading: false,
      error: false
    };
  }

  private ctaForMission(
    missionId: MissionId,
    input: MissionMapperInput,
    routeOverride?: string
  ): MissionCardModel['cta'] | undefined {
    if (missionId === 'EverythingOperational' || missionId === 'StaffCalm') {
      return undefined;
    }

    const routeMap: Partial<Record<MissionId, { route: string; action: 'route' | 'external-discord' | 'sync' }>> = {
      BotOffline: { route: '', action: 'external-discord' },
      SubscriptionExpired: { route: 'subscription', action: 'route' },
      PaymentRejected: { route: 'subscription', action: 'route' },
      SynchronizationStale: { route: '', action: 'sync' },
      SynchronizationNever: { route: '/servers', action: 'route' },
      TicketBacklogCritical: { route: 'tickets', action: 'route' },
      TicketBacklogElevated: { route: 'tickets', action: 'route' },
      SubscriptionExpiringSoon: { route: 'subscription', action: 'route' },
      CompleteSetupConnect: { route: '/servers', action: 'route' },
      CompleteSetupConfigure: { route: 'settings', action: 'route' },
      CompleteSetupFirstValue: { route: 'tickets', action: 'route' },
      InviteStaff: { route: 'staff', action: 'route' },
      EnableModule: { route: 'modules', action: 'route' },
      CreateWelcome: { route: 'settings', action: 'route' },
      CreateTicketPanel: { route: 'settings', action: 'route' },
      CreateReactionPanel: { route: 'reaction-roles', action: 'route' },
      PaymentRequired: { route: 'subscription', action: 'route' },
      ReviewLogs: { route: 'logs', action: 'route' }
    };

    const config = routeMap[missionId];
    if (!config) {
      return undefined;
    }

    const route = routeOverride ?? config.route;
    const labelKey = `overview.v2.mission.${this.missionKey(missionId)}.cta`;

    return {
      labelKey,
      route,
      action: config.action
    };
  }

  private missionKey(missionId: MissionId): string {
    const keys: Record<MissionId, string> = {
      BotOffline: 'botOffline',
      SubscriptionExpired: 'subscriptionExpired',
      PaymentRejected: 'paymentRejected',
      SynchronizationStale: 'syncStale',
      SynchronizationNever: 'syncNever',
      TicketBacklogCritical: 'ticketBacklogCritical',
      TicketBacklogElevated: 'ticketBacklog',
      SubscriptionExpiringSoon: 'subscriptionExpiring',
      CompleteSetupConnect: 'setupConnect',
      CompleteSetupConfigure: 'setupConfigure',
      CompleteSetupFirstValue: 'setupFirstWin',
      InviteStaff: 'inviteStaff',
      EnableModule: 'enableModule',
      CreateWelcome: 'createWelcome',
      CreateTicketPanel: 'createTicketPanel',
      CreateReactionPanel: 'createReactionPanel',
      PaymentRequired: 'paymentRequired',
      ReviewLogs: 'reviewLogs',
      EverythingOperational: 'allClear',
      StaffCalm: 'staffCalm'
    };

    return keys[missionId];
  }

  private defaultDismissible(missionId: MissionId): boolean {
    const snoozable: MissionId[] = [
      'SynchronizationStale',
      'TicketBacklogElevated',
      'SubscriptionExpiringSoon',
      'InviteStaff',
      'EnableModule',
      'CreateWelcome',
      'CreateTicketPanel',
      'CreateReactionPanel',
      'PaymentRequired',
      'ReviewLogs'
    ];

    return snoozable.includes(missionId);
  }

  private isMissionAllowed(missionId: MissionId, access: GuildAccess): boolean {
    const billingMissions: MissionId[] = [
      'SubscriptionExpired',
      'PaymentRejected',
      'SubscriptionExpiringSoon',
      'PaymentRequired'
    ];
    const setupMissions: MissionId[] = [
      'CompleteSetupConnect',
      'CompleteSetupConfigure',
      'CompleteSetupFirstValue',
      'EnableModule',
      'CreateWelcome',
      'CreateTicketPanel',
      'SynchronizationNever'
    ];
    const staffMissions: MissionId[] = [
      'InviteStaff',
      'CreateReactionPanel',
      'EnableModule',
      'PaymentRequired'
    ];

    if (billingMissions.includes(missionId) && !access.canManageSubscription) {
      return false;
    }

    if (setupMissions.includes(missionId) && !access.canManageSettings) {
      return false;
    }

    if (staffMissions.includes(missionId) && !access.canManageSettings && !access.canManageSubscription) {
      return missionId === 'StaffCalm' || missionId === 'EverythingOperational'
        || missionId.startsWith('Ticket')
        || missionId === 'BotOffline'
        || missionId === 'SynchronizationStale';
    }

    return true;
  }

  private isFirstValueAchieved(steps: ActivationStep[]): boolean {
    return steps.find(step => step.key === 'firstValue')?.completed ?? false;
  }

  private phaseComplete(steps: ActivationStep[], keys: string[]): boolean {
    return keys.every(key => steps.find(step => step.key === key)?.completed ?? false);
  }

  private isSyncStale(resourcesSyncedAt: string | null | undefined): boolean {
    if (!resourcesSyncedAt) {
      return true;
    }

    const syncedAt = new Date(resourcesSyncedAt).getTime();
    if (Number.isNaN(syncedAt)) {
      return true;
    }

    return Date.now() - syncedAt > SYNC_STALE_MS;
  }

  private isSubscriptionExpiringSoon(expiresAt: string | null | undefined, isExpired: boolean): boolean {
    if (!expiresAt || isExpired) {
      return false;
    }

    const days = this.daysUntil(expiresAt);
    return days > 0 && days <= SUBSCRIPTION_EXPIRING_DAYS;
  }

  private daysUntil(isoDate: string): number {
    const target = new Date(isoDate).getTime();
    if (Number.isNaN(target)) {
      return 0;
    }

    return Math.max(0, Math.ceil((target - Date.now()) / (24 * 60 * 60 * 1000)));
  }
}
