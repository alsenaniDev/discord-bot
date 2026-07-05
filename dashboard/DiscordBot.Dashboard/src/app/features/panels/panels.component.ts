import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { finalize, forkJoin, Subscription, switchMap, take, takeWhile, tap, timer } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { ToastService } from '../../core/services/toast.service';
import { DiscordChannel, DiscordRole, isTextChannel } from '../../core/models/guild.models';
import { GuildPanel, GuildPanelButton, PanelButtonActionType, PanelButtonStyle, SaveGuildPanel } from '../../core/models/command-panel.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import { PageWorkspaceHeroAction, PageWorkspaceHeroStat } from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';
import { GuildWorkflow } from '../../core/models/workflow.models';

const httpsUrlValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = String(control.value ?? '').trim();
  if (!value) return null;
  try { return new URL(value).protocol === 'https:' ? null : { httpsUrl: true }; }
  catch { return { httpsUrl: true }; }
};

const titleOrDescriptionValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const title = String(control.get('title')?.value ?? '').trim();
  const description = String(control.get('description')?.value ?? '').trim();
  return title || description ? null : { titleOrDescription: true };
};

@Component({
  selector: 'app-panels',
  templateUrl: './panels.component.html',
  styleUrls: ['../settings/settings.component.css', './panels.component.css']
})
export class PanelsComponent implements OnInit, OnDestroy {
  guildId = '';
  panels: GuildPanel[] = [];
  channels: DiscordChannel[] = [];
  roles: DiscordRole[] = [];
  workflows: GuildWorkflow[] = [];
  loading = true;
  saving = false;
  publishingId = '';
  error = '';
  editingId?: string;
  showEditor = false;

  readonly styles: PanelButtonStyle[] = ['Primary', 'Secondary', 'Success', 'Danger', 'Link'];
  readonly actions: PanelButtonActionType[] = ['CreateTicket', 'OpenUrl', 'SendMessage', 'AssignRole', 'StartWorkflow'];
  private readonly publishPolls = new Map<string, Subscription>();
  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    channelDiscordId: [''],
    title: ['', Validators.maxLength(256)],
    description: ['', Validators.maxLength(2000)],
    imageUrl: ['', httpsUrlValidator],
    isEnabled: [true],
    buttons: this.fb.array<FormGroup>([])
  }, { validators: titleOrDescriptionValidator });

  get buttons(): FormArray<FormGroup> { return this.form.controls.buttons; }
  get textChannels(): DiscordChannel[] { return this.channels.filter(isTextChannel); }
  get assignableRoles(): DiscordRole[] { return this.roles.filter(role => !role.isManaged); }
  get workspaceHeroStats(): PageWorkspaceHeroStat[] {
    return [
      { label: this.translate.instant('panels.stats.total'), value: String(this.panels.length) },
      { label: this.translate.instant('panels.stats.published'), value: String(this.panels.filter(x => x.publishStatus === 'Published').length) },
      { label: this.translate.instant('panels.stats.pending'), value: String(this.panels.filter(x => x.publishStatus === 'PendingPublish').length) }
    ];
  }
  get workspaceHeroPrimaryAction(): PageWorkspaceHeroAction {
    return { label: this.translate.instant(this.showEditor ? 'panels.actions.back' : 'panels.actions.create') };
  }

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private api: GuildService,
    private toast: ToastService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void { this.guildId = this.route.snapshot.paramMap.get('id') ?? ''; this.load(); }
  ngOnDestroy(): void { this.publishPolls.forEach(subscription => subscription.unsubscribe()); }
  onHeroAction(): void { this.showEditor ? this.cancel() : this.create(); }

  load(): void {
    this.loading = true;
    this.error = '';
    forkJoin({ panels: this.api.getPanels(this.guildId), channels: this.api.getChannels(this.guildId), roles: this.api.getRoles(this.guildId), workflows: this.api.getWorkflows(this.guildId) })
      .subscribe({
        next: data => { this.panels = data.panels; this.channels = data.channels; this.roles = data.roles; this.workflows = data.workflows; this.loading = false; },
        error: err => { this.loading = false; this.error = getApiErrorMessage(err, this.translate.instant('panels.messages.loadError')); }
      });
  }

  create(): void {
    this.editingId = undefined;
    this.showEditor = true;
    this.form.reset({ name: '', channelDiscordId: '', title: this.translate.instant('panels.defaults.title'), description: this.translate.instant('panels.defaults.description'), imageUrl: '', isEnabled: true });
    this.buttons.clear();
    this.addButton();
  }

  edit(panel: GuildPanel): void {
    this.editingId = panel.id;
    this.showEditor = true;
    this.form.patchValue(panel);
    this.buttons.clear();
    panel.buttons.forEach(button => this.addButton(button));
  }

  cancel(): void { this.showEditor = false; }

  addButton(value?: GuildPanelButton): void {
    if (this.buttons.length >= 25) return;
    const group = this.fb.group({
      id: [value?.id], label: [value?.label ?? '', [Validators.required, Validators.maxLength(80)]],
      emoji: [value?.emoji ?? ''], style: [value?.style ?? 'Success', Validators.required],
      actionType: [value?.actionType ?? 'CreateTicket', Validators.required], ticketTypeId: [value?.ticketTypeId],
      workflowId: [value?.workflowId],
      url: [value?.url ?? ''], responseMessage: [value?.responseMessage ?? ''], roleDiscordId: [value?.roleDiscordId ?? ''],
      isEnabled: [value?.isEnabled ?? true]
    });
    this.buttons.push(group);
    this.applyActionValidators(group);
  }

  actionChanged(group: FormGroup): void {
    if (group.controls['actionType'].value === 'OpenUrl') group.controls['style'].setValue('Link');
    else if (group.controls['style'].value === 'Link') group.controls['style'].setValue('Secondary');
    this.applyActionValidators(group);
  }

  removeButton(index: number): void { this.buttons.removeAt(index); }
  move(index: number, delta: number): void {
    const target = index + delta;
    if (target < 0 || target >= this.buttons.length) return;
    const item = this.buttons.at(index); this.buttons.removeAt(index); this.buttons.insert(target, item);
  }

  save(publish = false): void {
    if (publish && !this.form.controls.channelDiscordId.value) this.form.controls.channelDiscordId.setErrors({ requiredForPublish: true });
    if (publish && !this.form.controls.isEnabled.value) this.form.controls.isEnabled.setErrors({ requiredForPublish: true });
    if (this.form.invalid || this.buttons.length > 25) {
      this.form.markAllAsTouched();
      this.toast.error(this.translate.instant('panels.validation.fixErrors'));
      return;
    }
    this.saving = true;
    const raw = this.form.getRawValue();
    const payload: SaveGuildPanel = {
      name: raw.name!.trim(), channelDiscordId: raw.channelDiscordId!.trim(), title: raw.title!.trim(),
      description: raw.description!.trim(), imageUrl: raw.imageUrl?.trim() || null, isEnabled: !!raw.isEnabled,
      buttons: (raw.buttons as any[]).map((button, index) => ({ ...button, label: button.label.trim(), emoji: button.emoji?.trim() || null,
        url: button.url?.trim() || null, responseMessage: button.responseMessage?.trim() || null,
        roleDiscordId: button.roleDiscordId?.trim() || null, sortOrder: index, isEnabled: !!button.isEnabled }))
    };
    const request = this.editingId ? this.api.updatePanel(this.guildId, this.editingId, payload) : this.api.createPanel(this.guildId, payload);
    request.subscribe({
      next: panel => publish ? this.publishSaved(panel) : this.finishSave('panels.messages.saved'),
      error: err => { this.saving = false; this.toast.error(getApiErrorMessage(err, this.translate.instant('panels.messages.saveError'))); }
    });
  }

  publish(panel: GuildPanel): void {
    if (!panel.isEnabled) { this.toast.error(this.translate.instant('panels.validation.enableBeforePublish')); return; }
    this.publishingId = panel.id;
    this.api.publishPanel(this.guildId, panel.id).subscribe({
      next: () => {
        this.publishingId = '';
        panel.publishStatus = 'PendingPublish'; panel.refreshRequested = true;
        panel.lastPublishFailed = false; panel.lastPublishFailureReason = null;
        this.toast.success(this.translate.instant('panels.messages.publishRequested'));
        this.pollPublishStatus(panel.id);
      },
      error: err => { this.publishingId = ''; this.toast.error(getApiErrorMessage(err, this.translate.instant('panels.messages.publishError'))); }
    });
  }

  duplicate(panel: GuildPanel): void {
    this.edit(panel);
    this.editingId = undefined;
    this.form.controls.name.setValue(this.translate.instant('panels.duplicateName', { name: panel.name }));
  }

  remove(panel: GuildPanel): void {
    if (!window.confirm(this.translate.instant('panels.dialogs.deleteConfirm', { name: panel.name }))) return;
    this.api.deletePanel(this.guildId, panel.id).subscribe({
      next: () => { this.toast.success(this.translate.instant('panels.messages.deleted')); this.load(); },
      error: err => this.toast.error(getApiErrorMessage(err, this.translate.instant('panels.messages.deleteError')))
    });
  }

  channelName(id: string): string { const channel = this.channels.find(x => x.discordChannelId === id); return channel ? `#${channel.name}` : id; }
  roleName(id?: string | null): string { return this.roles.find(x => x.discordRoleId === id)?.name ?? id ?? ''; }
  statusKey(panel: GuildPanel): string {
    const keys = { NotPublished: 'panels.status.notPublished', PendingPublish: 'panels.status.requested', Published: 'panels.status.published', Failed: 'panels.status.failed' };
    return keys[panel.publishStatus];
  }
  statusTone(panel: GuildPanel): 'success' | 'warning' | 'danger' | 'neutral' {
    if (panel.publishStatus === 'Failed') return 'danger';
    if (panel.publishStatus === 'PendingPublish') return 'warning';
    return panel.publishStatus === 'Published' ? 'success' : 'neutral';
  }
  actionLabel(action: PanelButtonActionType): string { return this.translate.instant(`panels.actionTypes.${action}`); }
  styleLabel(style: PanelButtonStyle): string { return this.translate.instant(`panels.styles.${style}`); }

  private applyActionValidators(group: FormGroup): void {
    const action = group.controls['actionType'].value as PanelButtonActionType;
    group.controls['url'].setValidators(action === 'OpenUrl' ? [Validators.required, httpsUrlValidator] : []);
    group.controls['responseMessage'].setValidators(action === 'SendMessage' ? [Validators.required, Validators.maxLength(2000)] : []);
    group.controls['roleDiscordId'].setValidators(action === 'AssignRole' ? [Validators.required] : []);
    group.controls['workflowId'].setValidators(action === 'StartWorkflow' ? [Validators.required] : []);
    ['url', 'responseMessage', 'roleDiscordId', 'workflowId'].forEach(name => group.controls[name].updateValueAndValidity({ emitEvent: false }));
  }

  private publishSaved(panel: GuildPanel): void {
    this.api.publishPanel(this.guildId, panel.id).subscribe({
      next: () => { this.finishSave('panels.messages.savedAndPublishRequested'); this.pollPublishStatus(panel.id); },
      error: err => { this.saving = false; this.toast.error(getApiErrorMessage(err, this.translate.instant('panels.messages.publishError'))); }
    });
  }

  private finishSave(messageKey: string): void {
    this.saving = false; this.showEditor = false; this.toast.success(this.translate.instant(messageKey)); this.load();
  }

  private pollPublishStatus(panelId: string): void {
    this.publishPolls.get(panelId)?.unsubscribe();
    const subscription = timer(0, 2000).pipe(
      take(10),
      switchMap(() => this.api.getPanels(this.guildId)),
      tap(panels => { this.panels = panels; }),
      takeWhile(panels => panels.find(panel => panel.id === panelId)?.publishStatus === 'PendingPublish', true),
      finalize(() => this.publishPolls.delete(panelId))
    ).subscribe({ error: () => this.publishPolls.delete(panelId) });
    this.publishPolls.set(panelId, subscription);
  }
}
