import { Injectable } from '@angular/core';

const STORAGE_PREFIX = 'mission-snooze';

@Injectable({ providedIn: 'root' })
export class MissionDismissService {
  isSnoozed(missionId: string, guildId: string, userId?: string): boolean {
    const key = this.storageKey(missionId, guildId, userId);
    const raw = localStorage.getItem(key);
    if (!raw) {
      return false;
    }

    const snoozeUntil = Number(raw);
    if (Number.isNaN(snoozeUntil) || Date.now() >= snoozeUntil) {
      localStorage.removeItem(key);
      return false;
    }

    return true;
  }

  snoozeSevenDays(missionId: string, guildId: string, userId?: string): void {
    const key = this.storageKey(missionId, guildId, userId);
    const snoozeUntil = Date.now() + 7 * 24 * 60 * 60 * 1000;
    localStorage.setItem(key, String(snoozeUntil));
  }

  private storageKey(missionId: string, guildId: string, userId?: string): string {
    return `${STORAGE_PREFIX}:${userId ?? 'anonymous'}:${guildId}:${missionId}`;
  }
}
