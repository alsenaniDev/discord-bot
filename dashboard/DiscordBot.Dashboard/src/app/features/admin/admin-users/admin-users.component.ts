import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../core/services/admin.service';
import { AdminUser } from '../../../core/models/admin.models';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.css']
})
export class AdminUsersComponent implements OnInit {
  users: AdminUser[] = [];
  loading = true;
  error = '';

  constructor(
    private adminService: AdminService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = '';

    this.adminService.getUsers().subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('errors.loadUsers'));
        this.loading = false;
      }
    });
  }

  displayName(user: AdminUser): string {
    return user.globalName || user.username;
  }
}
