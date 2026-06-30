import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { ModerationCase, ModerationFilters, Warning, moderationCaseTypeLabel } from '../../core/models/moderation.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-moderation',
  templateUrl: './moderation.component.html',
  styleUrls: ['./moderation.component.css']
})
export class ModerationComponent implements OnInit {
  guildId = '';
  warnings: Warning[] = [];
  cases: ModerationCase[] = [];
  loading = true;
  error = '';

  filters: ModerationFilters = {
    targetUserId: '',
    type: '',
    from: '',
    to: ''
  };

  caseTypeLabel = moderationCaseTypeLabel;

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
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    const activeFilters = this.buildActiveFilters();

    forkJoin({
      warnings: this.guildService.getWarnings(this.guildId, activeFilters),
      cases: this.guildService.getModerationCases(this.guildId, activeFilters)
    }).subscribe({
      next: ({ warnings, cases }) => {
        this.warnings = warnings;
        this.cases = cases;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        const message = getApiErrorMessage(err, this.translate.instant('errors.loadModeration'));
        this.error = message;
        this.toast.error(message);
      }
    });
  }

  applyFilters(): void {
    this.loadData();
  }

  clearFilters(): void {
    this.filters = { targetUserId: '', type: '', from: '', to: '' };
    this.loadData();
  }

  private buildActiveFilters(): ModerationFilters {
    return {
      targetUserId: this.filters.targetUserId?.trim() || undefined,
      type: this.filters.type || undefined,
      from: this.filters.from || undefined,
      to: this.filters.to || undefined
    };
  }
}
