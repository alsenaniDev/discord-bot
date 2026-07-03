import { Injectable } from '@angular/core';
import { OverviewActivityItem } from '../models/guild.models';
import {
  ActivityTimelineGroupId,
  ActivityTimelineGroupModel,
  ActivityTimelineIconName,
  ActivityTimelineIconTone,
  ActivityTimelineMapperInput,
  ActivityTimelineModel,
  ActivityTimelineRow
} from '../models/mission-control.models';
import { GuildAccess } from '../models/staff.models';
import {
  extractModuleName,
  extractTicketNumber,
  moduleActivityParams,
  parseLegacyActivityMessage
} from './activity-timeline-legacy.parser';

const MAX_ITEMS = 5;

const GROUP_LABELS: Record<ActivityTimelineGroupId, string> = {
  today: 'overview.v2.activity.today',
  yesterday: 'overview.v2.activity.yesterday',
  earlier: 'overview.v2.activity.earlier'
};

interface ActivityTypeDefinition {
  icon: ActivityTimelineIconName;
  iconTone: ActivityTimelineIconTone;
  messageKey: string;
  routeSegment?: string;
  paramsFromMessage?: (message: string) => Record<string, string | number> | undefined;
}

/**
 * Converts raw overview activity items into structured i18n timeline rows.
 * Remove when API returns Type + Params directly (see activity-timeline-legacy.parser.ts).
 */
const ACTIVITY_TYPE_DEFINITIONS: Record<string, ActivityTypeDefinition> = {
  TicketCreated: {
    icon: 'tickets',
    iconTone: 'brand',
    messageKey: 'overview.v2.activity.ticketCreated',
    routeSegment: 'tickets',
    paramsFromMessage: message => {
      const number = extractTicketNumber(message);
      return number !== null ? { number } : undefined;
    }
  },
  TicketClosed: {
    icon: 'check-circle',
    iconTone: 'success',
    messageKey: 'overview.v2.activity.ticketClosed',
    routeSegment: 'tickets',
    paramsFromMessage: message => {
      const number = extractTicketNumber(message);
      return number !== null ? { number } : undefined;
    }
  },
  TicketReply: {
    icon: 'bell',
    iconTone: 'info',
    messageKey: 'overview.v2.activity.ticketReply',
    routeSegment: 'tickets',
    paramsFromMessage: message => {
      const number = extractTicketNumber(message);
      return number !== null ? { number } : undefined;
    }
  },
  ModuleEnabled: {
    icon: 'modules',
    iconTone: 'brand',
    messageKey: 'overview.v2.activity.moduleEnabled',
    routeSegment: 'modules',
    paramsFromMessage: message => moduleActivityParams(extractModuleName(message))
  },
  ModuleDisabled: {
    icon: 'x',
    iconTone: 'danger',
    messageKey: 'overview.v2.activity.moduleDisabled',
    routeSegment: 'modules',
    paramsFromMessage: message => moduleActivityParams(extractModuleName(message))
  },
  LogEntry: {
    icon: 'logs',
    iconTone: 'neutral',
    messageKey: 'overview.v2.activity.logEntryGeneric',
    routeSegment: 'logs'
  },
  MemberWarned: {
    icon: 'alert-circle',
    iconTone: 'warning',
    messageKey: 'overview.v2.activity.memberWarned',
    routeSegment: 'moderation'
  },
  StaffAdded: {
    icon: 'users',
    iconTone: 'brand',
    messageKey: 'overview.v2.activity.staffAdded',
    routeSegment: 'staff'
  },
  SubscriptionChange: {
    icon: 'subscription',
    iconTone: 'warning',
    messageKey: 'overview.v2.activity.subscriptionChange',
    routeSegment: 'subscription'
  }
};

@Injectable({ providedIn: 'root' })
export class ActivityTimelineMapperService {
  mapTimeline(input: ActivityTimelineMapperInput): ActivityTimelineModel {
    try {
      const rows = input.items
        .map((item, index) => this.mapItem(item, input.guildId, input.access, index))
        .filter((row): row is ActivityTimelineRow => row !== null)
        .sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime())
        .slice(0, MAX_ITEMS);

      return {
        groups: this.groupRows(rows),
        loading: false,
        error: false
      };
    } catch {
      return {
        groups: [],
        loading: false,
        error: true
      };
    }
  }

  createLoadingTimeline(): ActivityTimelineModel {
    return {
      groups: [],
      loading: true,
      error: false
    };
  }

  private mapItem(
    item: OverviewActivityItem,
    guildId: string,
    access: GuildAccess,
    index: number
  ): ActivityTimelineRow | null {
    if (!this.isVisibleForAccess(item.type, access)) {
      return null;
    }

    const guildRoute = (segment: string) => this.guildRoute(guildId, segment);
    const mapped = this.mapKnownType(item, guildRoute);
    if (mapped) {
      return {
        ...mapped,
        id: `${item.type}-${item.occurredAt}-${index}`
      };
    }

    const parsed = parseLegacyActivityMessage(item.message, {
      occurredAt: item.occurredAt,
      guildRoute
    });
    if (parsed) {
      return {
        ...parsed,
        id: `${item.type}-${item.occurredAt}-${index}`
      };
    }

    return {
      id: `${item.type}-${item.occurredAt}-${index}`,
      icon: 'clock',
      iconTone: 'neutral',
      messageKey: 'overview.v2.activity.unknown',
      occurredAt: item.occurredAt
    };
  }

  private mapKnownType(
    item: OverviewActivityItem,
    guildRoute: (segment: string) => string
  ): Omit<ActivityTimelineRow, 'id'> | null {
    const definition = ACTIVITY_TYPE_DEFINITIONS[item.type];
    if (!definition) {
      return null;
    }

    return {
      icon: definition.icon,
      iconTone: definition.iconTone,
      messageKey: definition.messageKey,
      messageParams: definition.paramsFromMessage?.(item.message),
      occurredAt: item.occurredAt,
      route: definition.routeSegment ? guildRoute(definition.routeSegment) : undefined
    };
  }

  private groupRows(rows: ActivityTimelineRow[]): ActivityTimelineGroupModel[] {
    const buckets: Record<ActivityTimelineGroupId, ActivityTimelineRow[]> = {
      today: [],
      yesterday: [],
      earlier: []
    };

    for (const row of rows) {
      buckets[this.resolveGroup(row.occurredAt)].push(row);
    }

    return (['today', 'yesterday', 'earlier'] as ActivityTimelineGroupId[])
      .filter(group => buckets[group].length > 0)
      .map(group => ({
        group,
        labelKey: GROUP_LABELS[group],
        rows: buckets[group]
      }));
  }

  private resolveGroup(isoDate: string): ActivityTimelineGroupId {
    const date = new Date(isoDate);
    if (Number.isNaN(date.getTime())) {
      return 'earlier';
    }

    const now = new Date();
    const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const startOfYesterday = new Date(startOfToday);
    startOfYesterday.setDate(startOfYesterday.getDate() - 1);

    if (date >= startOfToday) {
      return 'today';
    }

    if (date >= startOfYesterday) {
      return 'yesterday';
    }

    return 'earlier';
  }

  private isVisibleForAccess(type: string, access: GuildAccess): boolean {
    const moduleTypes = new Set(['ModuleEnabled', 'ModuleDisabled']);
    if (moduleTypes.has(type) && !access.canManageModules && !access.canManageSettings) {
      return false;
    }

    return true;
  }

  private guildRoute(guildId: string, segment: string): string {
    return `/guilds/${guildId}/${segment}`;
  }
}
