import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../core/services/admin.service';
import { AdminStats } from '../../../core/models/admin.models';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';

@Component({
  selector: 'app-admin-home',
  templateUrl: './admin-home.component.html',
  styleUrls: ['./admin-home.component.css']
})
export class AdminHomeComponent implements OnInit {
  stats: AdminStats | null = null;
  loading = true;
  error = '';

  constructor(
    private adminService: AdminService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.loading = true;
    this.error = '';

    this.adminService.getStats().subscribe({
      next: stats => {
        this.stats = stats;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('errors.loadStats'));
        this.loading = false;
      }
    });
  }
}
