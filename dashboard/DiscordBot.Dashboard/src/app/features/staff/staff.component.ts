import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { AddGuildStaffRequest, GuildStaffMember, GuildStaffRole } from '../../core/models/staff.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-staff',
  templateUrl: './staff.component.html',
  styleUrls: ['./staff.component.css']
})
export class StaffComponent implements OnInit {
  guildId = '';
  staff: GuildStaffMember[] = [];
  loading = true;
  error = '';
  saving = false;
  discordUserId = '';
  role: GuildStaffRole = 'Moderator';

  readonly roles: GuildStaffRole[] = ['Moderator', 'Manager'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private guildService: GuildService,
    private guildContext: GuildContextService,
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.guildId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.guildId) {
      this.router.navigate(['/servers']);
      return;
    }

    this.guildContext.ensureGuild(this.guildId, this.guildService);
    this.loadStaff();
  }

  loadStaff(): void {
    this.loading = true;
    this.error = '';

    this.guildService.getStaff(this.guildId).subscribe({
      next: staff => {
        this.staff = staff;
        this.loading = false;
      },
      error: err => {
        this.error = getApiErrorMessage(err, this.translate.instant('staff.loadError'));
        this.loading = false;
      }
    });
  }

  addStaff(): void {
    const discordUserId = this.discordUserId.trim();
    if (!discordUserId || this.saving) {
      return;
    }

    this.saving = true;
    const request: AddGuildStaffRequest = {
      discordUserId,
      role: this.role
    };

    this.guildService.addStaff(this.guildId, request).subscribe({
      next: member => {
        this.staff = [...this.staff, member];
        this.discordUserId = '';
        this.role = 'Moderator';
        this.saving = false;
        this.toast.success(this.translate.instant('staff.added'));
      },
      error: err => {
        this.saving = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('staff.addError')));
      }
    });
  }

  removeStaff(member: GuildStaffMember): void {
    this.guildService.removeStaff(this.guildId, member.id).subscribe({
      next: () => {
        this.staff = this.staff.filter(s => s.id !== member.id);
        this.toast.success(this.translate.instant('staff.removed'));
      },
      error: err => {
        this.toast.error(getApiErrorMessage(err, this.translate.instant('staff.removeError')));
      }
    });
  }

  roleLabel(role: GuildStaffRole): string {
    return this.translate.instant(`staff.roles.${role.toLowerCase()}`);
  }
}
