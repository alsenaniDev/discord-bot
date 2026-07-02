import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GuildSettings, GuildSummary, DiscordChannel, DiscordRole, GuildOverview, RequestResourceSyncResponse, UpdateGuildSettings, GuildProfile, UpdateGuildProfile, ModerationPermissionRole, CreateModerationPermissionRole, UpdateModerationPermissionRole } from '../models/guild.models';
import { GuildMember } from '../models/guild-member.models';
import { Ticket } from '../models/ticket.models';
import { AutoReplyRule, CreateAutoReplyRule, UpdateAutoReplyRule } from '../models/auto-reply.models';
import { ModerationCase, ModerationFilters, Warning } from '../models/moderation.models';
import { GuildModule, UpdateGuildModuleRequest } from '../models/module.models';
import { LogEntry, LogFilters } from '../models/log.models';
import { ReactionRolePanel } from '../models/reaction-role.models';
import { GuildSubscription, SubscriptionPlan } from '../models/subscription.models';
import { PlanUpgradeRequest, CreatePlanUpgradeRequest } from '../models/upgrade-request.models';
import {
  CreateGuildPermissionRoleRequest,
  GuildAccess,
  GuildPermissionRole,
  UpdateGuildPermissionRoleRequest
} from '../models/staff.models';

@Injectable({ providedIn: 'root' })
export class GuildService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getGuilds(): Observable<GuildSummary[]> {
    return this.http.get<GuildSummary[]>(`${this.baseUrl}/api/guilds`);
  }

  getOverview(guildId: string): Observable<GuildOverview> {
    return this.http.get<GuildOverview>(`${this.baseUrl}/api/guilds/${guildId}/overview`);
  }

  getSettings(guildId: string): Observable<GuildSettings> {
    return this.http.get<GuildSettings>(`${this.baseUrl}/api/guilds/${guildId}/settings`);
  }

  updateSettings(guildId: string, settings: UpdateGuildSettings): Observable<GuildSettings> {
    return this.http.put<GuildSettings>(
      `${this.baseUrl}/api/guilds/${guildId}/settings`,
      settings
    );
  }

  getTickets(guildId: string): Observable<Ticket[]> {
    return this.http.get<Ticket[]>(`${this.baseUrl}/api/guilds/${guildId}/tickets`);
  }

  closeTicket(guildId: string, ticketId: string): Observable<Ticket> {
    return this.http.patch<Ticket>(
      `${this.baseUrl}/api/guilds/${guildId}/tickets/${ticketId}/close`,
      {}
    );
  }

  sendTicketMessage(guildId: string, ticketId: string, content: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.baseUrl}/api/guilds/${guildId}/tickets/${ticketId}/messages`,
      { content }
    );
  }

  getAutoReplies(guildId: string): Observable<AutoReplyRule[]> {
    return this.http.get<AutoReplyRule[]>(`${this.baseUrl}/api/guilds/${guildId}/auto-replies`);
  }

  createAutoReply(guildId: string, request: CreateAutoReplyRule): Observable<AutoReplyRule> {
    return this.http.post<AutoReplyRule>(`${this.baseUrl}/api/guilds/${guildId}/auto-replies`, request);
  }

  updateAutoReply(guildId: string, ruleId: string, request: UpdateAutoReplyRule): Observable<AutoReplyRule> {
    return this.http.put<AutoReplyRule>(
      `${this.baseUrl}/api/guilds/${guildId}/auto-replies/${ruleId}`,
      request
    );
  }

  deleteAutoReply(guildId: string, ruleId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/guilds/${guildId}/auto-replies/${ruleId}`);
  }

  getChannels(guildId: string): Observable<DiscordChannel[]> {
    return this.http.get<DiscordChannel[]>(`${this.baseUrl}/api/guilds/${guildId}/channels`);
  }

  getCategories(guildId: string): Observable<DiscordChannel[]> {
    return this.http.get<DiscordChannel[]>(`${this.baseUrl}/api/guilds/${guildId}/categories`);
  }

  getRoles(guildId: string): Observable<DiscordRole[]> {
    return this.http.get<DiscordRole[]>(`${this.baseUrl}/api/guilds/${guildId}/roles`);
  }

  getMembers(guildId: string, search = ''): Observable<GuildMember[]> {
    const params: Record<string, string> = {};
    if (search.trim()) {
      params['search'] = search.trim();
    }

    return this.http.get<GuildMember[]>(`${this.baseUrl}/api/guilds/${guildId}/members`, { params });
  }

  requestResourceSync(guildId: string): Observable<RequestResourceSyncResponse> {
    return this.http.post<RequestResourceSyncResponse>(
      `${this.baseUrl}/api/guilds/${guildId}/sync-resources`,
      {}
    );
  }

  getWarnings(guildId: string, filters: ModerationFilters = {}): Observable<Warning[]> {
    const params = this.buildModerationParams(filters);
    return this.http.get<Warning[]>(`${this.baseUrl}/api/guilds/${guildId}/warnings`, { params });
  }

  getModerationCases(guildId: string, filters: ModerationFilters = {}): Observable<ModerationCase[]> {
    const params = this.buildModerationParams(filters);
    return this.http.get<ModerationCase[]>(`${this.baseUrl}/api/guilds/${guildId}/moderation-cases`, { params });
  }

  private buildModerationParams(filters: ModerationFilters): Record<string, string> {
    const params: Record<string, string> = {};

    if (filters.targetUserId?.trim()) {
      params['targetUserId'] = filters.targetUserId.trim();
    }
    if (filters.type) {
      params['type'] = filters.type;
    }
    if (filters.from) {
      params['from'] = new Date(filters.from).toISOString();
    }
    if (filters.to) {
      params['to'] = new Date(filters.to).toISOString();
    }

    return params;
  }

  getModules(guildId: string): Observable<GuildModule[]> {
    return this.http.get<GuildModule[]>(`${this.baseUrl}/api/guilds/${guildId}/modules`);
  }

  updateModule(
    guildId: string,
    moduleKey: string,
    request: UpdateGuildModuleRequest
  ): Observable<GuildModule> {
    return this.http.put<GuildModule>(
      `${this.baseUrl}/api/guilds/${guildId}/modules/${encodeURIComponent(moduleKey)}`,
      request
    );
  }

  getLogs(guildId: string, filters: LogFilters = {}): Observable<LogEntry[]> {
    const params = this.buildLogParams(filters);
    return this.http.get<LogEntry[]>(`${this.baseUrl}/api/guilds/${guildId}/logs`, { params });
  }

  clearLogs(guildId: string, confirmation: string): Observable<{ deletedCount: number }> {
    return this.http.request<{ deletedCount: number }>('DELETE', `${this.baseUrl}/api/guilds/${guildId}/logs`, {
      body: { confirmation }
    });
  }

  private buildLogParams(filters: LogFilters): Record<string, string> {
    const params: Record<string, string> = {};

    if (filters.type) {
      params['type'] = filters.type;
    }
    if (filters.from) {
      params['from'] = new Date(filters.from).toISOString();
    }
    if (filters.to) {
      const end = new Date(filters.to);
      end.setHours(23, 59, 59, 999);
      params['to'] = end.toISOString();
    }
    if (filters.search?.trim()) {
      params['search'] = filters.search.trim();
    }
    if (filters.userId?.trim()) {
      params['userId'] = filters.userId.trim();
    }

    return params;
  }

  getReactionRoles(guildId: string): Observable<ReactionRolePanel[]> {
    return this.http.get<ReactionRolePanel[]>(`${this.baseUrl}/api/guilds/${guildId}/reaction-roles`);
  }

  deactivateReactionRole(guildId: string, reactionRoleId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(
      `${this.baseUrl}/api/guilds/${guildId}/reaction-roles/${reactionRoleId}`
    );
  }

  getPlans(): Observable<SubscriptionPlan[]> {
    return this.http.get<SubscriptionPlan[]>(`${this.baseUrl}/api/plans`);
  }

  getSubscription(guildId: string): Observable<GuildSubscription> {
    return this.http.get<GuildSubscription>(`${this.baseUrl}/api/guilds/${guildId}/subscription`);
  }

  getGuildAccess(guildId: string): Observable<GuildAccess> {
    return this.http.get<GuildAccess>(`${this.baseUrl}/api/guilds/${guildId}/access`);
  }

  getUpgradeRequests(guildId: string): Observable<PlanUpgradeRequest[]> {
    return this.http.get<PlanUpgradeRequest[]>(
      `${this.baseUrl}/api/guilds/${guildId}/subscription/upgrade-requests`
    );
  }

  createUpgradeRequest(
    guildId: string,
    request: CreatePlanUpgradeRequest
  ): Observable<PlanUpgradeRequest> {
    return this.http.post<PlanUpgradeRequest>(
      `${this.baseUrl}/api/guilds/${guildId}/subscription/upgrade-requests`,
      request
    );
  }

  getStaff(guildId: string): Observable<GuildPermissionRole[]> {
    return this.http.get<GuildPermissionRole[]>(`${this.baseUrl}/api/guilds/${guildId}/permission-roles`);
  }

  addStaff(guildId: string, request: CreateGuildPermissionRoleRequest): Observable<GuildPermissionRole> {
    return this.http.post<GuildPermissionRole>(`${this.baseUrl}/api/guilds/${guildId}/permission-roles`, request);
  }

  updatePermissionRole(
    guildId: string,
    roleId: string,
    request: UpdateGuildPermissionRoleRequest
  ): Observable<GuildPermissionRole> {
    return this.http.put<GuildPermissionRole>(
      `${this.baseUrl}/api/guilds/${guildId}/permission-roles/${roleId}`,
      request
    );
  }

  removeStaff(guildId: string, staffId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(
      `${this.baseUrl}/api/guilds/${guildId}/permission-roles/${staffId}`
    );
  }

  getProfile(guildId: string): Observable<GuildProfile> {
    return this.http.get<GuildProfile>(`${this.baseUrl}/api/guilds/${guildId}/profile`);
  }

  updateProfile(guildId: string, profile: UpdateGuildProfile): Observable<GuildProfile> {
    return this.http.put<GuildProfile>(`${this.baseUrl}/api/guilds/${guildId}/profile`, profile);
  }

  getModerationPermissionRoles(guildId: string): Observable<ModerationPermissionRole[]> {
    return this.http.get<ModerationPermissionRole[]>(
      `${this.baseUrl}/api/guilds/${guildId}/moderation/permission-roles`
    );
  }

  createModerationPermissionRole(
    guildId: string,
    request: CreateModerationPermissionRole
  ): Observable<ModerationPermissionRole> {
    return this.http.post<ModerationPermissionRole>(
      `${this.baseUrl}/api/guilds/${guildId}/moderation/permission-roles`,
      request
    );
  }

  updateModerationPermissionRole(
    guildId: string,
    roleId: string,
    request: UpdateModerationPermissionRole
  ): Observable<ModerationPermissionRole> {
    return this.http.put<ModerationPermissionRole>(
      `${this.baseUrl}/api/guilds/${guildId}/moderation/permission-roles/${roleId}`,
      request
    );
  }

  deleteModerationPermissionRole(guildId: string, roleId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/api/guilds/${guildId}/moderation/permission-roles/${roleId}`
    );
  }
}
