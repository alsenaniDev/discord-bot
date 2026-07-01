import {
  Component,
  ElementRef,
  forwardRef,
  HostListener,
  Input,
  OnChanges,
  SimpleChanges
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { GuildService } from '../../../core/services/guild.service';
import { GuildMember } from '../../../core/models/guild-member.models';

@Component({
  selector: 'app-member-select',
  templateUrl: './member-select.component.html',
  styleUrls: ['./member-select.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MemberSelectComponent),
      multi: true
    }
  ]
})
export class MemberSelectComponent implements ControlValueAccessor, OnChanges {
  @Input() guildId = '';
  @Input() label = '';
  @Input() placeholder = '';
  @Input() allowClear = true;

  query = '';
  members: GuildMember[] = [];
  selectedMember: GuildMember | null = null;
  open = false;
  loading = false;
  loadError = false;
  disabled = false;

  get hasSelection(): boolean {
    return !!this.value;
  }

  private value = '';
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(
    private guildService: GuildService,
    private elementRef: ElementRef<HTMLElement>
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['guildId'] && !changes['guildId'].firstChange) {
      this.resetSelection();
      this.members = [];
    }
  }

  writeValue(value: string | null): void {
    this.value = value?.trim() ?? '';
    if (!this.value) {
      this.selectedMember = null;
      this.query = '';
      return;
    }

    this.query = this.value;
    this.ensureSelectedMemberLoaded();
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onFocus(): void {
    if (this.disabled) {
      return;
    }

    this.open = true;
    this.onTouched();
    this.loadMembers(this.query);
  }

  onQueryChange(value: string): void {
    this.query = value;
    this.open = true;
    this.loadMembers(value);
  }

  selectMember(member: GuildMember): void {
    this.selectedMember = member;
    this.query = this.formatMember(member);
    this.value = member.discordUserId;
    this.open = false;
    this.onChange(this.value);
    this.onTouched();
  }

  clearSelection(): void {
    this.resetSelection();
    this.onChange('');
    this.onTouched();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.open = false;
    }
  }

  private resetSelection(): void {
    this.selectedMember = null;
    this.query = '';
    this.value = '';
  }

  private loadMembers(search: string): void {
    if (!this.guildId) {
      this.members = [];
      return;
    }

    this.loading = true;
    this.loadError = false;

    this.guildService.getMembers(this.guildId, search).subscribe({
      next: members => {
        this.members = members;
        this.loading = false;
        this.syncSelectedMember(members);
      },
      error: () => {
        this.members = [];
        this.loading = false;
        this.loadError = true;
      }
    });
  }

  private ensureSelectedMemberLoaded(): void {
    if (!this.guildId || !this.value) {
      return;
    }

    if (this.selectedMember?.discordUserId === this.value) {
      this.query = this.formatMember(this.selectedMember);
      return;
    }

    this.guildService.getMembers(this.guildId, this.value).subscribe({
      next: members => {
        this.syncSelectedMember(members);
        if (this.selectedMember) {
          this.query = this.formatMember(this.selectedMember);
        }
      }
    });
  }

  private syncSelectedMember(members: GuildMember[]): void {
    if (!this.value) {
      return;
    }

    const match = members.find(member => member.discordUserId === this.value);
    if (match) {
      this.selectedMember = match;
    }
  }

  formatMember(member: GuildMember): string {
    const handle = member.globalName && member.globalName !== member.username
      ? `${member.displayName} (@${member.username})`
      : member.displayName;
    return `${handle} · ${member.discordUserId}`;
  }
}
