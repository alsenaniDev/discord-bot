import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { GuildService } from '../../core/services/guild.service';
import { ToastService } from '../../core/services/toast.service';
import { DiscordRole } from '../../core/models/guild.models';
import { GuildWorkflow, SaveWorkflow, WorkflowAnswer, WorkflowApprovalAction, WorkflowQuestion, WorkflowQuestionOption, WorkflowSubmission } from '../../core/models/workflow.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';
import { PageWorkspaceHeroAction, PageWorkspaceHeroStat } from '../../shared/ui/page-workspace-hero/page-workspace-hero.models';

@Component({ selector: 'app-workflows', templateUrl: './workflows.component.html', styleUrls: ['../settings/settings.component.css', './workflows.component.css'] })
export class WorkflowsComponent implements OnInit {
  guildId = ''; workflows: GuildWorkflow[] = []; submissions: WorkflowSubmission[] = []; roles: DiscordRole[] = [];
  loading = true; saving = false; error = ''; view: 'list' | 'editor' | 'submissions' = 'list'; editingId?: string; selected?: WorkflowSubmission;
  readonly types = ['Application', 'Survey', 'Report', 'Custom']; readonly questionTypes = ['ShortText', 'LongText', 'Number', 'YesNo', 'SingleChoice'];
  readonly policies = ['AllowMultiple', 'BlockWhilePending', 'BlockAfterApproved', 'CooldownAfterRejected', 'OneSubmissionEver']; readonly actionTypes = ['AddRole', 'RemoveRole', 'SendDirectMessage'];
  form = this.fb.group({ name: ['', Validators.required], description: [''], type: ['Application'], startMode: ['DirectMessage'], isEnabled: [true], requireConfirmation: [false], confirmationTitle: [''], confirmationMessage: [''], confirmationConfirmButtonText: ['Continue'], confirmationCancelButtonText: ['Cancel'], duplicatePolicy: ['BlockWhilePending'], cooldownHours: [null as number | null], maxSubmissionsPerUser: [null as number | null], successMessage: [''], rejectionMessage: [''], questions: this.fb.array<FormGroup>([]), approvalActions: this.fb.array<FormGroup>([]) });
  get questions(): FormArray<FormGroup> { return this.form.controls.questions; } get actions(): FormArray<FormGroup> { return this.form.controls.approvalActions; }
  get stats(): PageWorkspaceHeroStat[] { return [{ label: this.t.instant('workflows.stats.total'), value: String(this.workflows.length) }, { label: this.t.instant('workflows.stats.enabled'), value: String(this.workflows.filter(x => x.isEnabled).length) }, { label: this.t.instant('workflows.stats.pending'), value: String(this.submissions.filter(x => x.status === 'Pending').length) }]; }
  get heroAction(): PageWorkspaceHeroAction { return { label: this.t.instant(this.view === 'editor' ? 'workflows.actions.back' : 'workflows.actions.create') }; }
  constructor(private route: ActivatedRoute, private fb: FormBuilder, private api: GuildService, private toast: ToastService, private t: TranslateService) { }
  ngOnInit(): void { this.guildId = this.route.snapshot.paramMap.get('id') ?? ''; this.load(); }
  load(): void { this.loading = true; forkJoin({ workflows: this.api.getWorkflows(this.guildId), submissions: this.api.getWorkflowSubmissions(this.guildId), roles: this.api.getRoles(this.guildId) }).subscribe({ next: x => { this.workflows = x.workflows; this.submissions = x.submissions; this.roles = x.roles; this.loading = false; }, error: e => { this.loading = false; this.error = getApiErrorMessage(e, this.t.instant('workflows.messages.loadError')); } }); }
  heroClick(): void { this.view === 'editor' ? this.showList() : this.create(); }
  showList(): void { this.view = 'list'; this.editingId = undefined; }
  showSubmissions(): void {
    this.view = 'submissions'; this.selected = undefined;
    this.api.getWorkflowSubmissions(this.guildId).subscribe({
      next: submissions => { this.submissions = submissions; },
      error: error => this.toast.error(getApiErrorMessage(error, this.t.instant('workflows.messages.loadError')))
    });
  }
  create(): void { this.view = 'editor'; this.editingId = undefined; this.form.reset({ name: '', description: '', type: 'Application', startMode: 'DirectMessage', isEnabled: true, requireConfirmation: false, confirmationTitle: '', confirmationMessage: '', confirmationConfirmButtonText: this.t.instant('workflows.defaults.confirm'), confirmationCancelButtonText: this.t.instant('workflows.defaults.cancel'), duplicatePolicy: 'BlockWhilePending', cooldownHours: null, maxSubmissionsPerUser: null, successMessage: '', rejectionMessage: '' }); this.questions.clear(); this.actions.clear(); this.addQuestion(); }
  edit(w: GuildWorkflow): void { this.view = 'editor'; this.editingId = w.id; this.form.patchValue(w as any); this.questions.clear(); w.questions.forEach(x => this.addQuestion(x)); this.actions.clear(); w.approvalActions.forEach(x => this.addAction(x)); }
  duplicate(w: GuildWorkflow): void { this.edit(w); this.editingId = undefined; this.form.controls.name.setValue(this.t.instant('workflows.duplicateName', { name: w.name })); }
  addQuestion(q?: WorkflowQuestion): void {
    if (this.questions.length >= 10) return;
    const options = this.fb.array<FormGroup>((q?.options ?? []).map(x => this.optionGroup(x)), [Validators.minLength(q?.type === 'SingleChoice' ? 2 : 0), Validators.maxLength(5)]);
    const group = this.fb.group({ id: [q?.id], sortOrder: [q?.sortOrder ?? this.questions.length], label: [q?.label ?? '', Validators.required], helpText: [q?.helpText ?? ''], type: [q?.type ?? 'ShortText'], isRequired: [q?.isRequired ?? true], minLength: [q?.minLength], maxLength: [q?.maxLength], placeholder: [q?.placeholder ?? ''], options });
    this.questions.push(group);
    group.controls.type.valueChanges.subscribe(type => {
      options.setValidators([Validators.minLength(type === 'SingleChoice' ? 2 : 0), Validators.maxLength(5)]);
      if (type === 'SingleChoice' && options.length < 2) while (options.length < 2) this.addOption(group);
      options.updateValueAndValidity();
    });
    if (q?.type === 'SingleChoice' && options.length < 2) while (options.length < 2) this.addOption(group);
  }
  optionGroup(option?: WorkflowQuestionOption): FormGroup { return this.fb.group({ label: [option?.label ?? '', [Validators.required, Validators.maxLength(80)]], value: [option?.value ?? '', Validators.required], sortOrder: [option?.sortOrder ?? 0] }); }
  questionOptions(question: FormGroup): FormArray<FormGroup> { return question.controls['options'] as FormArray<FormGroup>; }
  addOption(question: FormGroup): void { const options = this.questionOptions(question); if (options.length < 5) options.push(this.optionGroup({ label: '', value: '', sortOrder: options.length })); }
  removeOption(question: FormGroup, index: number): void { this.questionOptions(question).removeAt(index); }
  addAction(a?: WorkflowApprovalAction): void { if (this.actions.length >= 10) return; this.actions.push(this.fb.group({ id: [a?.id], sortOrder: [a?.sortOrder ?? this.actions.length], actionType: [a?.actionType ?? 'AddRole'], roleDiscordId: [a?.roleDiscordId ?? ''], messageText: [a?.messageText ?? ''], isEnabled: [a?.isEnabled ?? true] })); }
  remove(array: FormArray, index: number): void { array.removeAt(index); } move(array: FormArray, index: number, delta: number): void { const n = index + delta; if (n < 0 || n >= array.length) return; const x = array.at(index); array.removeAt(index); array.insert(n, x); }
  save(): void { if (this.form.invalid) { this.form.markAllAsTouched(); this.toast.error(this.t.instant('workflows.validation.fix')); return; } this.saving = true; const r: any = this.form.getRawValue(); const payload: SaveWorkflow = { ...r, name: r.name.trim(), description: r.description?.trim() || null, questions: r.questions.map((x: any, i: number) => ({ ...x, sortOrder: i, helpText: x.helpText?.trim() || null, placeholder: x.placeholder?.trim() || null, options: x.type === 'SingleChoice' ? x.options.map((o: any, oi: number) => ({ label: o.label.trim(), value: o.value.trim(), sortOrder: oi })) : [] })), approvalActions: r.approvalActions.map((x: any, i: number) => ({ ...x, sortOrder: i, roleDiscordId: x.roleDiscordId || null, messageText: x.messageText?.trim() || null })) }; const req = this.editingId ? this.api.updateWorkflow(this.guildId, this.editingId, payload) : this.api.createWorkflow(this.guildId, payload); req.subscribe({ next: () => { this.saving = false; this.toast.success(this.t.instant('workflows.messages.saved')); this.showList(); this.load(); }, error: e => { this.saving = false; this.toast.error(getApiErrorMessage(e, this.t.instant('workflows.messages.saveError'))); } }); }
  delete(w: GuildWorkflow): void { if (!confirm(this.t.instant('workflows.dialogs.delete', { name: w.name }))) return; this.api.deleteWorkflow(this.guildId, w.id).subscribe({ next: () => this.load(), error: e => this.toast.error(getApiErrorMessage(e, this.t.instant('workflows.messages.deleteError'))) }); }
  selectSubmission(x: WorkflowSubmission): void { this.selected = x; } review(approve: boolean): void { if (!this.selected) return; const note = prompt(this.t.instant('workflows.review.notePrompt')) ?? undefined; const req = approve ? this.api.approveWorkflowSubmission(this.guildId, this.selected.id, note) : this.api.rejectWorkflowSubmission(this.guildId, this.selected.id, note); req.subscribe({ next: x => { this.selected = x; this.toast.success(this.t.instant(approve ? 'workflows.messages.approved' : 'workflows.messages.rejected')); this.load(); }, error: e => this.toast.error(getApiErrorMessage(e, this.t.instant('workflows.messages.reviewError'))) }); }
  label(group: FormGroup): string { return group.controls['label']?.value || this.t.instant('workflows.preview.untitled'); } roleName(id: string): string { return this.roles.find(x => x.discordRoleId === id)?.name ?? id; }
  answerDisplay(submission: WorkflowSubmission, answer: WorkflowAnswer): string {
    const question = this.workflows.find(x => x.id === submission.workflowId)?.questions.find(x => x.id === answer.questionId);
    const type = answer.questionType || question?.type;
    if (type === 'YesNo') return this.t.instant(answer.value === 'yes' ? 'workflows.answers.yes' : answer.value === 'no' ? 'workflows.answers.no' : 'common.emptyValue');
    if (type === 'SingleChoice') return answer.displayValue || question?.options.find(x => x.value === answer.value)?.label || answer.value;
    return answer.value;
  }
}
