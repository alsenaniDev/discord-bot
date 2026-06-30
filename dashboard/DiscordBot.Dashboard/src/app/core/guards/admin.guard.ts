import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AdminGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate() {
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login']);
      return of(false);
    }

    return this.auth.getCurrentUser().pipe(
      map(user => {
        if (user.isAdmin) {
          return true;
        }

        this.router.navigate(['/servers']);
        return false;
      }),
      catchError(() => {
        this.router.navigate(['/login']);
        return of(false);
      })
    );
  }
}
