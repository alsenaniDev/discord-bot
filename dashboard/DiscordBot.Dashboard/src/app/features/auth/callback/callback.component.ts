import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({
  selector: 'app-auth-callback',
  templateUrl: './callback.component.html',
  styleUrls: ['./callback.component.css']
})
export class CallbackComponent implements OnInit {
  loading = true;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const code = this.route.snapshot.queryParamMap.get('code');

    if (!code) {
      this.loading = false;
      this.error = 'Missing login code. Please try again from the login page.';
      return;
    }

    this.auth.exchangeCode(code).subscribe({
      next: () => this.router.navigate(['/servers']),
      error: err => {
        this.loading = false;
        this.error = getApiErrorMessage(err, 'Login failed. The code may have expired — please try again.');
      }
    });
  }
}
