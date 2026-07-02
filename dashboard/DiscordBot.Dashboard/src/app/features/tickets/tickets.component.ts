import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import {
  Ticket,
  TicketTimelineEvent,
  displayChannelLabel,
  displayMemberLabel,
  isTicketOpen,
  ticketStatusLabel,
  ticketTimelineEventLabel
} from '../../core/models/ticket.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

@Component({
  selector: 'app-tickets',
  templateUrl: './tickets.component.html',
  styleUrls: ['./tickets.component.css']
})
export class TicketsComponent implements OnInit {
  guildId = '';
  tickets: Ticket[] = [];
  loading = true;
  error = '';
  closingTicketId = '';
  replyingTicketId = '';
  replyDrafts: Record<string, string> = {};
  sendingReplyId = '';
  expandedTimelineTicketId = '';
  timelineLoadingId = '';
  timelineErrors: Record<string, string> = {};
  timelines: Record<string, TicketTimelineEvent[]> = {};

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
    this.loadTickets();
  }

  loadTickets(): void {
    this.loading = true;
    this.error = '';

    this.guildService.getTickets(this.guildId).subscribe({
      next: tickets => {
        this.tickets = tickets;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        const message = getApiErrorMessage(err, this.translate.instant('errors.loadTickets'));
        this.error = message;
        this.toast.error(message);
      }
    });
  }

  closeTicket(ticket: Ticket): void {
    if (!isTicketOpen(ticket.status)) {
      return;
    }

    const confirmMessage = this.translate.instant('tickets.closeConfirm', {
      number: ticket.ticketNumber
    });
    if (!window.confirm(confirmMessage)) {
      return;
    }

    this.closingTicketId = ticket.id;

    this.guildService.closeTicket(this.guildId, ticket.id).subscribe({
      next: () => {
        this.closingTicketId = '';
        this.toast.success(this.translate.instant('tickets.closeSuccess'));
        this.loadTickets();
      },
      error: err => {
        this.closingTicketId = '';
        this.toast.error(getApiErrorMessage(err, this.translate.instant('tickets.closeError')));
      }
    });
  }

  isOpen(ticket: Ticket): boolean {
    return isTicketOpen(ticket.status);
  }

  isClosing(ticket: Ticket): boolean {
    return this.closingTicketId === ticket.id;
  }

  isReplying(ticket: Ticket): boolean {
    return this.replyingTicketId === ticket.id;
  }

  toggleReply(ticket: Ticket): void {
    if (!this.isOpen(ticket)) {
      return;
    }

    this.replyingTicketId = this.replyingTicketId === ticket.id ? '' : ticket.id;
    if (!this.replyDrafts[ticket.id]) {
      this.replyDrafts[ticket.id] = '';
    }
  }

  sendReply(ticket: Ticket): void {
    const content = (this.replyDrafts[ticket.id] ?? '').trim();
    if (!content) {
      this.toast.error(this.translate.instant('tickets.replyEmpty'));
      return;
    }

    this.sendingReplyId = ticket.id;
    this.guildService.sendTicketMessage(this.guildId, ticket.id, content).subscribe({
      next: () => {
        this.sendingReplyId = '';
        this.replyDrafts[ticket.id] = '';
        this.replyingTicketId = '';
        this.toast.success(this.translate.instant('tickets.replySuccess'));
        if (this.expandedTimelineTicketId === ticket.id) {
          this.loadTimeline(ticket, true);
        }
      },
      error: err => {
        this.sendingReplyId = '';
        this.toast.error(getApiErrorMessage(err, this.translate.instant('tickets.replyError')));
      }
    });
  }

  isSendingReply(ticket: Ticket): boolean {
    return this.sendingReplyId === ticket.id;
  }

  toggleTimeline(ticket: Ticket): void {
    if (this.expandedTimelineTicketId === ticket.id) {
      this.expandedTimelineTicketId = '';
      return;
    }

    this.expandedTimelineTicketId = ticket.id;
    if (!this.timelines[ticket.id]) {
      this.loadTimeline(ticket);
    }
  }

  isTimelineExpanded(ticket: Ticket): boolean {
    return this.expandedTimelineTicketId === ticket.id;
  }

  isTimelineLoading(ticket: Ticket): boolean {
    return this.timelineLoadingId === ticket.id;
  }

  loadTimeline(ticket: Ticket, force = false): void {
    if (!force && this.timelines[ticket.id]) {
      return;
    }

    this.timelineLoadingId = ticket.id;
    delete this.timelineErrors[ticket.id];

    this.guildService.getTicketTimeline(this.guildId, ticket.id).subscribe({
      next: events => {
        this.timelines[ticket.id] = events;
        this.timelineLoadingId = '';
      },
      error: err => {
        this.timelineLoadingId = '';
        this.timelineErrors[ticket.id] = getApiErrorMessage(
          err,
          this.translate.instant('tickets.timeline.loadError')
        );
      }
    });
  }

  timelineFor(ticket: Ticket): TicketTimelineEvent[] {
    return this.timelines[ticket.id] ?? [];
  }

  timelineError(ticket: Ticket): string {
    return this.timelineErrors[ticket.id] ?? '';
  }

  eventLabel(event: TicketTimelineEvent): string {
    return ticketTimelineEventLabel(event.eventType);
  }

  statusLabel(status: number | string): string {
    return ticketStatusLabel(status);
  }

  displayMember(name?: string | null, id?: string | null): string {
    return displayMemberLabel(name, id);
  }

  displayChannel(name?: string | null, id?: string | null): string {
    return displayChannelLabel(name, id);
  }
}
