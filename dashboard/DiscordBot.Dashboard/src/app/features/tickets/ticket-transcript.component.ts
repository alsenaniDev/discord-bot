import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import {
  TicketConversationEntryReadModel,
  TicketTranscriptMetadataReadModel,
  TicketTranscriptReadModel,
  displayMemberLabel,
  isTicketOpen,
  ticketDeliveryStatusLabel,
  ticketStatusLabel,
  ticketTimelineEventLabel
} from '../../core/models/ticket.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-ticket-transcript',
  templateUrl: './ticket-transcript.component.html',
  styleUrls: ['./ticket-transcript.component.css']
})
export class TicketTranscriptComponent implements OnInit {
  guildId = '';
  ticketId = '';
  loading = true;
  loadingMore = false;
  error = '';
  metadata: TicketTranscriptMetadataReadModel | null = null;
  entries: TicketConversationEntryReadModel[] = [];
  hasMore = false;
  nextCursorOccurredAt?: string | null;
  nextCursorEventId?: string | null;

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
    this.ticketId = this.route.snapshot.paramMap.get('ticketId') ?? '';

    if (!this.guildId || !this.ticketId) {
      this.router.navigate(['/servers']);
      return;
    }

    this.guildContext.ensureGuild(this.guildId, this.guildService);
    this.loadTranscript();
  }

  loadTranscript(append = false): void {
    if (append) {
      this.loadingMore = true;
    } else {
      this.loading = true;
      this.error = '';
    }

    this.guildService
      .getTicketTranscript(this.guildId, this.ticketId, {
        limit: 50,
        cursorOccurredAt: append ? this.nextCursorOccurredAt ?? undefined : undefined,
        cursorEventId: append ? this.nextCursorEventId ?? undefined : undefined
      })
      .subscribe({
        next: (page: TicketTranscriptReadModel) => {
          this.metadata = page.metadata;
          this.entries = append ? [...this.entries, ...page.entries] : page.entries;
          this.hasMore = page.hasMore;
          this.nextCursorOccurredAt = page.nextCursorOccurredAt;
          this.nextCursorEventId = page.nextCursorEventId;
          this.loading = false;
          this.loadingMore = false;
        },
        error: err => {
          this.loading = false;
          this.loadingMore = false;
          const message = getApiErrorMessage(err, this.translate.instant('tickets.transcript.loadError'));
          this.error = message;
          this.toast.error(message);
        }
      });
  }

  backToTickets(): void {
    this.router.navigate(['/guilds', this.guildId, 'tickets']);
  }

  get isOpen(): boolean {
    return this.metadata ? isTicketOpen(this.metadata.status) : false;
  }

  statusLabel(status: number | string): string {
    return ticketStatusLabel(status);
  }

  eventLabel(event: TicketConversationEntryReadModel): string {
    return ticketTimelineEventLabel(event.eventType);
  }

  deliveryLabel(event: TicketConversationEntryReadModel): string {
    return ticketDeliveryStatusLabel(event.deliveryStatus);
  }

  showDeliveryBadge(event: TicketConversationEntryReadModel): boolean {
    return event.deliveryStatus !== 'None';
  }

  displayMember(name?: string | null, id?: string | null): string {
    return displayMemberLabel(name, id);
  }
}
