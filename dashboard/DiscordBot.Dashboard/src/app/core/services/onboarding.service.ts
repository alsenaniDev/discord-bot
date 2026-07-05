import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DiscordGuildOnboarding, OnboardingStatus } from '../models/onboarding.models';

@Injectable({ providedIn: 'root' })
export class OnboardingService {
  private readonly baseUrl = `${environment.apiUrl}/api/onboarding`;

  constructor(private http: HttpClient) { }

  getStatus(): Observable<OnboardingStatus> {
    return this.http.get<OnboardingStatus>(`${this.baseUrl}/status`);
  }

  getDiscordGuilds() {
    return this.http.get<DiscordGuildOnboarding[]>(
      `${this.baseUrl}/discord-guilds`
    );
  }
}
