import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GuildAccess } from '../models/staff.models';

@Injectable({ providedIn: 'root' })
export class GuildAccessService {
  private readonly baseUrl = environment.apiUrl;
  private readonly accessByGuildId = new Map<string, BehaviorSubject<GuildAccess | null>>();

  constructor(private http: HttpClient) {}

  getAccess(guildId: string): Observable<GuildAccess> {
    return this.http.get<GuildAccess>(`${this.baseUrl}/api/guilds/${guildId}/access`);
  }

  loadAccess(guildId: string): Observable<GuildAccess> {
    return this.getAccess(guildId).pipe(
      tap(access => this.setAccess(guildId, access))
    );
  }

  access$(guildId: string): Observable<GuildAccess | null> {
    if (!this.accessByGuildId.has(guildId)) {
      this.accessByGuildId.set(guildId, new BehaviorSubject<GuildAccess | null>(null));
    }

    return this.accessByGuildId.get(guildId)!.asObservable();
  }

  currentAccess(guildId: string): GuildAccess | null {
    return this.accessByGuildId.get(guildId)?.value ?? null;
  }

  setAccess(guildId: string, access: GuildAccess | null): void {
    if (!this.accessByGuildId.has(guildId)) {
      this.accessByGuildId.set(guildId, new BehaviorSubject<GuildAccess | null>(access));
      return;
    }

    this.accessByGuildId.get(guildId)!.next(access);
  }

  clearAccess(guildId?: string): void {
    if (guildId) {
      this.accessByGuildId.delete(guildId);
      return;
    }

    this.accessByGuildId.clear();
  }
}
