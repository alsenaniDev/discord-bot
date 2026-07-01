import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loading = false;
  error = '';
  readonly apiUrl = environment.apiUrl;
  readonly dashboardOrigin = typeof window !== 'undefined' ? window.location.origin : '';

  constructor(private auth: AuthService, private router: Router) {}

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/servers']);
    }
  }

  loginWithDiscord(): void {
    this.loading = true;
    this.error = '';

    this.auth.getDiscordLoginUrl().subscribe({
      next: res => {
        window.location.href = res.url;
      },
      error: err => {
        this.loading = false;
        this.error = getApiErrorMessage(err, 'Could not start Discord login. Is the API running?');
      }
    });
  }
}
