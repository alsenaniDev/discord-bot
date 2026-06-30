import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loading = false;
  error = '';

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
