import {
  ActivityTimelineIconTone,
  ActivityTimelineRow
} from '../models/mission-control.models';

/**
 * Temporary legacy parsers for raw English API activity messages.
 * Delete this file when the backend returns structured Type + Params.
 */
export const LEGACY_ACTIVITY_PATTERNS = {
  ticketClosed: /Ticket #(\d+) closed/i,
  ticketReply:
    /(?:reply|replied).*(?:ticket #|#)(\d+)|Ticket #(\d+).*(?:reply|replied)/i,
  ticketOpened: /Ticket #(\d+)(?:\s+opened)?/i,
  moduleEnabled: /^(.+?) module enabled$/i,
  moduleDisabled: /^(.+?) module disabled$/i,
  ticketNumber: /#(\d+)/,
  moduleName: /^(.+?) module (?:enabled|disabled)$/i
} as const;

export interface LegacyActivityParseContext {
  occurredAt: string;
  guildRoute: (segment: string) => string;
}

type ParsedLegacyRow = Omit<ActivityTimelineRow, 'id'>;

export function extractTicketNumber(message: string): number | null {
  const match = message.match(LEGACY_ACTIVITY_PATTERNS.ticketNumber);
  if (!match) {
    return null;
  }

  const number = Number(match[1]);
  return Number.isNaN(number) ? null : number;
}

export function extractModuleName(message: string): string | null {
  const match = message.match(LEGACY_ACTIVITY_PATTERNS.moduleName);
  return match ? match[1].trim() : null;
}

const MODULE_NAME_ALIASES: Record<string, string> = {
  welcome: 'welcome',
  Welcome: 'welcome',
  tickets: 'tickets',
  Tickets: 'tickets',
  logs: 'logs',
  Logs: 'logs',
  moderation: 'moderation',
  Moderation: 'moderation',
  'reaction roles': 'reaction-roles',
  'Reaction Roles': 'reaction-roles',
  'auto role': 'auto-role',
  'Auto Role': 'auto-role'
};

export function resolveModuleNameKey(rawName: string): string | null {
  const normalized = rawName.trim();
  if (MODULE_NAME_ALIASES[normalized]) {
    return MODULE_NAME_ALIASES[normalized];
  }

  const lower = normalized.toLowerCase();
  for (const [alias, key] of Object.entries(MODULE_NAME_ALIASES)) {
    if (alias.toLowerCase() === lower) {
      return key;
    }
  }

  return null;
}

export function moduleActivityParams(
  rawName: string | null
): Record<string, string> | undefined {
  if (!rawName) {
    return undefined;
  }

  const moduleNameKey = resolveModuleNameKey(rawName);
  return moduleNameKey ? { moduleNameKey } : { moduleName: rawName };
}

export function parseLegacyActivityMessage(
  message: string,
  ctx: LegacyActivityParseContext
): ParsedLegacyRow | null {
  const ticketClosedMatch = message.match(LEGACY_ACTIVITY_PATTERNS.ticketClosed);
  if (ticketClosedMatch) {
    return legacyRow({
      icon: 'check-circle',
      iconTone: 'success',
      messageKey: 'overview.v2.activity.ticketClosed',
      messageParams: { number: ticketClosedMatch[1] },
      occurredAt: ctx.occurredAt,
      route: ctx.guildRoute('tickets')
    });
  }

  const ticketReplyMatch = message.match(LEGACY_ACTIVITY_PATTERNS.ticketReply);
  if (ticketReplyMatch) {
    const number = ticketReplyMatch[1] ?? ticketReplyMatch[2];
    return legacyRow({
      icon: 'bell',
      iconTone: 'info',
      messageKey: 'overview.v2.activity.ticketReply',
      messageParams: { number },
      occurredAt: ctx.occurredAt,
      route: ctx.guildRoute('tickets')
    });
  }

  const ticketOpenedMatch = message.match(LEGACY_ACTIVITY_PATTERNS.ticketOpened);
  if (ticketOpenedMatch) {
    return legacyRow({
      icon: 'tickets',
      iconTone: 'brand',
      messageKey: 'overview.v2.activity.ticketCreated',
      messageParams: { number: ticketOpenedMatch[1] },
      occurredAt: ctx.occurredAt,
      route: ctx.guildRoute('tickets')
    });
  }

  const moduleEnabledMatch = message.match(LEGACY_ACTIVITY_PATTERNS.moduleEnabled);
  if (moduleEnabledMatch) {
    return legacyRow({
      icon: 'modules',
      iconTone: 'brand',
      messageKey: 'overview.v2.activity.moduleEnabled',
      messageParams: moduleActivityParams(moduleEnabledMatch[1].trim()),
      occurredAt: ctx.occurredAt,
      route: ctx.guildRoute('modules')
    });
  }

  const moduleDisabledMatch = message.match(LEGACY_ACTIVITY_PATTERNS.moduleDisabled);
  if (moduleDisabledMatch) {
    return legacyRow({
      icon: 'x',
      iconTone: 'danger',
      messageKey: 'overview.v2.activity.moduleDisabled',
      messageParams: moduleActivityParams(moduleDisabledMatch[1].trim()),
      occurredAt: ctx.occurredAt,
      route: ctx.guildRoute('modules')
    });
  }

  return null;
}

function legacyRow(row: {
  icon: ParsedLegacyRow['icon'];
  iconTone: ActivityTimelineIconTone;
  messageKey: string;
  messageParams?: Record<string, string | number>;
  occurredAt: string;
  route?: string;
}): ParsedLegacyRow {
  return row;
}
