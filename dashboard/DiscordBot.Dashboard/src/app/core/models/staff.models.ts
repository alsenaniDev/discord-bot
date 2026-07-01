export type GuildStaffRole = 'Moderator' | 'Manager';

export interface GuildStaffMember {
  id: string;
  guildId: string;
  discordUserId: string;
  role: GuildStaffRole;
  createdAt: string;
  createdByDiscordUserId: string;
}

export interface AddGuildStaffRequest {
  discordUserId: string;
  role: GuildStaffRole;
}

export interface GuildAccess {
  isOwner: boolean;
  isPlatformAdmin: boolean;
  staffRole?: string | null;
  canManageSettings: boolean;
  canManageModules: boolean;
  canManageSubscription: boolean;
  canManageStaff: boolean;
  canAccessModeration: boolean;
  canAccessLogs: boolean;
  canAccessTickets: boolean;
  canAccessOverview: boolean;
}
