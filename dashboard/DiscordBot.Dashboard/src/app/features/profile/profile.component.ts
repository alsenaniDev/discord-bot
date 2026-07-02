import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  guildId = '';
  form!: FormGroup;
  loading = true;
  saving = false;
  error = '';
  serverName = '';
  iconUrl: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
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

    this.form = this.fb.group({
      displayName: ['', [Validators.maxLength(256)]],
      description: ['', [Validators.maxLength(1000)]],
      communityType: ['', [Validators.maxLength(100)]],
      supportMessage: ['', [Validators.maxLength(1000)]],
      rulesUrl: ['', [Validators.maxLength(2048)]],
      websiteUrl: ['', [Validators.maxLength(2048)]]
    });

    this.loadProfile();
  }

  loadProfile(): void {
    this.loading = true;
    this.error = '';

    this.guildService.getProfile(this.guildId).subscribe({
      next: profile => {
        this.serverName = profile.name;
        this.iconUrl = profile.iconUrl ?? null;
        this.form.patchValue({
          displayName: profile.displayName ?? '',
          description: profile.description ?? '',
          communityType: profile.communityType ?? '',
          supportMessage: profile.supportMessage ?? '',
          rulesUrl: profile.rulesUrl ?? '',
          websiteUrl: profile.websiteUrl ?? ''
        });
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.error = getApiErrorMessage(err, this.translate.instant('profile.loadError'));
      }
    });
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving) {
      return;
    }

    this.saving = true;
    const value = this.form.value;

    this.guildService.updateProfile(this.guildId, {
      displayName: value.displayName?.trim() || null,
      description: value.description?.trim() || null,
      communityType: value.communityType?.trim() || null,
      supportMessage: value.supportMessage?.trim() || null,
      rulesUrl: value.rulesUrl?.trim() || null,
      websiteUrl: value.websiteUrl?.trim() || null
    }).subscribe({
      next: () => {
        this.saving = false;
        this.toast.success(this.translate.instant('profile.saved'));
      },
      error: err => {
        this.saving = false;
        this.toast.error(getApiErrorMessage(err, this.translate.instant('profile.saveError')));
      }
    });
  }

  fieldError(controlName: string): string | null {
    const control = this.form.get(controlName);
    if (!control || !control.touched || !control.errors) {
      return null;
    }

    if (control.errors['maxlength']) {
      return this.translate.instant('settings.validation.maxLength');
    }

    return this.translate.instant('settings.validation.invalid');
  }
}
