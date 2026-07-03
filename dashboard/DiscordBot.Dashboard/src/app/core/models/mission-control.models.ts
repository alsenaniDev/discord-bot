export type MissionIconName =
  | 'bot'
  | 'subscription'
  | 'refresh'
  | 'tickets'
  | 'settings'
  | 'external'
  | 'check-circle'
  | 'users'
  | 'modules'
  | 'logs'
  | 'roles'
  | 'clock'
  | 'cloud-off'
  | 'alert-circle';

export type MissionId =
  | 'BotOffline'
  | 'SubscriptionExpired'
  | 'PaymentRejected'
  | 'SynchronizationStale'
  | 'SynchronizationNever'
  | 'TicketBacklogCritical'
  | 'TicketBacklogElevated'
  | 'SubscriptionExpiringSoon'
  | 'CompleteSetupConnect'
  | 'CompleteSetupConfigure'
  | 'CompleteSetupFirstValue'
  | 'InviteStaff'
  | 'EnableModule'
  | 'CreateWelcome'
  | 'CreateTicketPanel'
  | 'CreateReactionPanel'
  | 'PaymentRequired'
  | 'ReviewLogs'
  | 'EverythingOperational'
  | 'StaffCalm';

export type MissionSeverity = 'critical' | 'warning' | 'info' | 'neutral';

export type MissionCtaAction = 'route' | 'external-discord' | 'sync';

export interface MissionCta {
  labelKey: string;
  route?: string;
  action: MissionCtaAction;
}

export type BeginnerPhaseKey = 'connect' | 'configure' | 'firstWin';

export interface BeginnerProgressPhase {
  key: BeginnerPhaseKey;
  labelKey: string;
  status: 'completed' | 'current' | 'upcoming';
}

export interface MissionCardModel {
  missionId: MissionId;
  severity: MissionSeverity;
  icon: MissionIconName;
  titleKey: string;
  descriptionKey: string;
  descriptionParams?: Record<string, string | number>;
  cta?: MissionCta;
  dismissible: boolean;
  showProgress: boolean;
  progressPhases?: BeginnerProgressPhase[];
  progressPercent?: number;
  loading: boolean;
  error: boolean;
}

export interface StatusStripModel {
  showPlan: boolean;
  planName: string;
  showStaffAccess: boolean;
  botOnline: boolean;
  syncing: boolean;
  resourcesSyncedAt: string | null | undefined;
}

export interface MissionControlHeaderState {
  visible: boolean;
  loading: boolean;
  model: StatusStripModel | null;
}

export interface MissionMapperInput {
  overview: import('./guild.models').GuildOverview;
  experience: import('./guild.models').GuildOverviewExperience;
  access: import('./staff.models').GuildAccess;
  guildId: string;
}

export type PulseMetricTone = 'default' | 'success' | 'warning' | 'danger' | 'muted';

export type CommunityPulseMode = 'beginner' | 'veteran';

export interface PulseHealthValue {
  score: number;
  levelLabelKey: string;
}

export interface PulseMetricModel {
  id: string;
  labelKey: string;
  tone: PulseMetricTone;
  valueKey?: string;
  valueParams?: Record<string, string | number>;
  health?: PulseHealthValue;
}

export interface CommunityPulseModel {
  mode: CommunityPulseMode;
  loading: boolean;
  metrics: PulseMetricModel[];
}

export interface CommunityPulseMapperInput {
  overview: import('./guild.models').GuildOverview;
  experience: import('./guild.models').GuildOverviewExperience;
  modules: import('./module.models').GuildModule[];
  access: import('./staff.models').GuildAccess;
}

export type ActivityTimelineGroupId = 'today' | 'yesterday' | 'earlier';

export type ActivityTimelineIconName =
  | 'tickets'
  | 'logs'
  | 'modules'
  | 'alert-circle'
  | 'users'
  | 'subscription'
  | 'check-circle'
  | 'bell'
  | 'x'
  | 'clock';

export type ActivityTimelineIconTone =
  | 'brand'
  | 'success'
  | 'info'
  | 'warning'
  | 'neutral'
  | 'danger';

export interface ActivityTimelineRow {
  id: string;
  icon: ActivityTimelineIconName;
  iconTone: ActivityTimelineIconTone;
  messageKey: string;
  messageParams?: Record<string, string | number>;
  occurredAt: string;
  route?: string;
}

export interface ActivityTimelineGroupModel {
  group: ActivityTimelineGroupId;
  labelKey: string;
  rows: ActivityTimelineRow[];
}

export interface ActivityTimelineModel {
  groups: ActivityTimelineGroupModel[];
  loading: boolean;
  error: boolean;
}

export interface ActivityTimelineMapperInput {
  items: import('./guild.models').OverviewActivityItem[];
  guildId: string;
  access: import('./staff.models').GuildAccess;
}

export type ContextDrawerModuleStatus = 'enabled' | 'warning' | 'disabled';

export interface ContextDrawerModuleRow {
  key: string;
  nameKey: string;
  status: ContextDrawerModuleStatus;
  route: string;
}

export interface ContextDrawerSubscriptionModel {
  planName: string;
  expiresAt: string | null | undefined;
  showRenew: boolean;
  renewRoute: string;
  manageRoute: string;
}

export interface ContextDrawerSetupModel {
  phaseLabelKey: string;
  remainingSteps: number;
  progressPercent: number;
  resumeRoute: string;
}

export interface ContextDrawerHelpLink {
  id: string;
  labelKey: string;
  url: string;
  external: boolean;
}

export interface ContextDrawerSuggestionRow {
  id: string;
  titleKey: string;
  route: string;
}

export interface ContextDrawerModel {
  loading: boolean;
  modules: ContextDrawerModuleRow[];
  showSubscription: boolean;
  subscription: ContextDrawerSubscriptionModel | null;
  showSetup: boolean;
  setup: ContextDrawerSetupModel | null;
  helpLinks: ContextDrawerHelpLink[];
  suggestions: ContextDrawerSuggestionRow[];
  sectionHints: string[];
}

export interface ContextDrawerMapperInput {
  guildId: string;
  modules: import('./module.models').GuildModule[];
  experience: import('./guild.models').GuildOverviewExperience;
  access: import('./staff.models').GuildAccess;
}
