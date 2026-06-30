import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { GuildSummary } from '../models/guild.models';
import { GuildService } from './guild.service';

@Injectable({ providedIn: 'root' })
export class GuildContextService {
  private readonly selectedGuildSubject = new BehaviorSubject<GuildSummary | null>(null);
  readonly selectedGuild$ = this.selectedGuildSubject.asObservable();

  selectGuild(guild: GuildSummary): void {
    this.selectedGuildSubject.next(guild);
  }

  clearGuild(): void {
    this.selectedGuildSubject.next(null);
  }

  ensureGuild(guildId: string, guildService: GuildService): void {
    const current = this.selectedGuildSubject.value;
    if (current?.id === guildId) {
      return;
    }

    guildService.getGuilds().subscribe({
      next: guilds => {
        const found = guilds.find(g => g.id === guildId);
        if (found) {
          this.selectGuild(found);
        }
      }
    });
  }

  get discordServerUrl(): string | null {
    const guild = this.selectedGuildSubject.value;
    if (!guild?.discordGuildId) {
      return null;
    }

    return `https://discord.com/channels/${guild.discordGuildId}`;
  }
}
