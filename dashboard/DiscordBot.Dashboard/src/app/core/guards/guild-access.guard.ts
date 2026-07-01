import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, map, of } from 'rxjs';
import { GuildAccessService } from '../services/guild-access.service';

export type GuildAccessRequirement = 'owner' | 'moderation';

@Injectable({ providedIn: 'root' })
export class GuildAccessGuard implements CanActivate {
  constructor(
    private guildAccessService: GuildAccessService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): Observable<boolean | UrlTree> {
    const guildId = route.paramMap.get('id');
    const requirement = (route.data['guildAccess'] as GuildAccessRequirement | undefined) ?? 'owner';

    if (!guildId) {
      return of(this.router.createUrlTree(['/servers']));
    }

    return this.guildAccessService.loadAccess(guildId).pipe(
      map(access => {
        const allowed =
          requirement === 'moderation'
            ? access.canAccessModeration
            : access.canManageSettings;

        if (allowed) {
          return true;
        }

        if (access.canAccessModeration) {
          return this.router.createUrlTree(['/guilds', guildId, 'moderation']);
        }

        return this.router.createUrlTree(['/servers']);
      })
    );
  }
}
