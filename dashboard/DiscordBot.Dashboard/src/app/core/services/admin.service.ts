import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminGuildSummary,
  AdminStats,
  AdminUser,
  UpdateAdminGuildSubscriptionRequest
} from '../models/admin.models';
import { GuildSubscription } from '../models/subscription.models';
import { AdminPlanUpgradeRequest, ReviewPlanUpgradeRequest } from '../models/upgrade-request.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly baseUrl = `${environment.apiUrl}/api/admin`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<AdminStats> {
    return this.http.get<AdminStats>(`${this.baseUrl}/stats`);
  }

  getGuilds(): Observable<AdminGuildSummary[]> {
    return this.http.get<AdminGuildSummary[]>(`${this.baseUrl}/guilds`);
  }

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.baseUrl}/users`);
  }

  updateGuildSubscription(
    guildId: string,
    request: UpdateAdminGuildSubscriptionRequest
  ): Observable<GuildSubscription> {
    return this.http.put<GuildSubscription>(`${this.baseUrl}/guilds/${guildId}/subscription`, request);
  }

  getUpgradeRequests(): Observable<AdminPlanUpgradeRequest[]> {
    return this.http.get<AdminPlanUpgradeRequest[]>(`${this.baseUrl}/upgrade-requests`);
  }

  approveUpgradeRequest(
    requestId: string,
    body: ReviewPlanUpgradeRequest
  ): Observable<AdminPlanUpgradeRequest> {
    return this.http.post<AdminPlanUpgradeRequest>(
      `${this.baseUrl}/upgrade-requests/${requestId}/approve`,
      body
    );
  }

  rejectUpgradeRequest(
    requestId: string,
    body: ReviewPlanUpgradeRequest
  ): Observable<AdminPlanUpgradeRequest> {
    return this.http.post<AdminPlanUpgradeRequest>(
      `${this.baseUrl}/upgrade-requests/${requestId}/reject`,
      body
    );
  }

  extendGuildSubscription(
    guildId: string,
    months: number
  ): Observable<GuildSubscription> {
    return this.http.post<GuildSubscription>(
      `${this.baseUrl}/guilds/${guildId}/subscription/extend`,
      { months }
    );
  }

  cancelGuildSubscription(guildId: string): Observable<GuildSubscription> {
    return this.http.post<GuildSubscription>(
      `${this.baseUrl}/guilds/${guildId}/subscription/cancel`,
      {}
    );
  }
}
