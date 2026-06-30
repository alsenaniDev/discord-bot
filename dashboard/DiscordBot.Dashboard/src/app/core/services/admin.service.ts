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
}
