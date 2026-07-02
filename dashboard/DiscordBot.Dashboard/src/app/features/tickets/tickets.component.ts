import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { GuildAccessService } from '../../core/services/guild-access.service';
import { ToastService } from '../../core/services/toast.service';
import { GuildAccess } from '../../core/models/staff.models';
import {
  PaginatedTicketConversationReadModel,
  TicketConversationEntryReadModel,
  TicketSummaryReadModel,
  displayChannelLabel,
  displayMemberLabel,
  isTicketOpen,
  ticketDeliveryStatusLabel,
  ticketStatusLabel,
  ticketTimelineEventLabel
} from '../../core/models/ticket.models';
import { getApiErrorMessage } from '../../core/utils/api-error.util';

type StatusFilter = 'all' | 'Open' | 'Closed';

@Component({
  selector: 'app-tickets',
  templateUrl: './tickets.component.html',
  styleUrls: ['./tickets.component.css']
})
export class TicketsComponent implements OnInit {
  guildId = '';
  tickets: TicketSummaryReadModel[] = [];
  loading = true;
  error = '';
  page = 1;
  pageSize = 20;
  totalPages = 0;
  totalCount = 0;
  statusFilter: StatusFilter = 'all';
  access: GuildAccess | null = null;

  closingTicketId = '';
  replyingTicketId = '';
  replyDrafts: Record<string, string> = {};
  sendingReplyId = '';

  expandedConversationTicketId = '';
  conversationLoadingId = '';
  conversationLoadMoreId = '';
  conversationErrors: Record<string, string> = {};
  conversations: Record<string, TicketConversationEntryReadModel[]> = {};
  conversationMeta: Record<string, Pick<PaginatedTicketConversationReadModel, 'hasMore' | 'nextCursorOccurredAt' | 'nextCursorEventId'>> = {};

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private guildService: GuildService,
    private guildContext: GuildContextService,
    private guildAccessService: GuildAccessService,
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
    this.guildAccessService.loadAccess(this.guildId).subscribe({
      next: access => {
        this.access = access;
      }
    });
    this.loadTickets();
  }

  loadTickets(): void {
    this.loading = true;
    this.error = '';

    this.guildService
      .getTicketSummaries(this.guildId, {
        page: this.page,
        pageSize: this.pageSize,
        sort: 'lastActivity',
        status: this.statusFilter === 'all' ? undefined : this.statusFilter
      })
      .subscribe({
        next: page => {
          this.tickets = page.items;
          this.page = page.page;
          this.pageSize = page.pageSize;
          this.totalCount = page.totalCount;
          this.totalPages = page.totalPages;
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

  onStatusFilterChange(value: string): void {
    this.statusFilter = value as StatusFilter;
    this.page = 1;
    this.loadTickets();
  }

  goToPage(nextPage: number): void {
    if (nextPage < 1 || (this.totalPages > 0 && nextPage > this.totalPages)) {
      return;
    }

    this.page = nextPage;
    this.loadTickets();
  }

  closeTicket(ticket: TicketSummaryReadModel): void {
    if (!this.canClose || !isTicketOpen(ticket.status)) {
      return;
    }

    const confirmMessage = this.translate.instant('tickets.closeConfirm', {
      number: ticket.ticketNumber
    });
    if (!window.confirm(confirmMessage)) {
      return;
    }

    this.closingTicketId = ticket.ticketId;

    this.guildService.closeTicket(this.guildId, ticket.ticketId).subscribe({
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

  toggleReply(ticket: TicketSummaryReadModel): void {
    if (!this.canReply || !isTicketOpen(ticket.status)) {
      return;
    }

    this.replyingTicketId = this.replyingTicketId === ticket.ticketId ? '' : ticket.ticketId;
    if (!this.replyDrafts[ticket.ticketId]) {
      this.replyDrafts[ticket.ticketId] = '';
    }
  }

  sendReply(ticket: TicketSummaryReadModel): void {
    if (!this.canReply) {
      return;
    }

    const content = (this.replyDrafts[ticket.ticketId] ?? '').trim();
    if (!content) {
      this.toast.error(this.translate.instant('tickets.replyEmpty'));
      return;
    }

    this.sendingReplyId = ticket.ticketId;
    this.guildService.sendTicketMessage(this.guildId, ticket.ticketId, content).subscribe({
      next: () => {
        this.sendingReplyId = '';
        this.replyDrafts[ticket.ticketId] = '';
        this.replyingTicketId = '';
        this.toast.success(this.translate.instant('tickets.replySuccess'));
        this.loadTickets();
        if (this.expandedConversationTicketId === ticket.ticketId) {
          this.loadConversation(ticket, true);
        }
      },
      error: err => {
        this.sendingReplyId = '';
        this.toast.error(getApiErrorMessage(err, this.translate.instant('tickets.replyError')));
      }
    });
  }

  toggleConversation(ticket: TicketSummaryReadModel): void {
    if (this.expandedConversationTicketId === ticket.ticketId) {
      this.expandedConversationTicketId = '';
      return;
    }

    this.expandedConversationTicketId = ticket.ticketId;
    if (!this.conversations[ticket.ticketId]) {
      this.loadConversation(ticket);
    }
  }

  loadConversation(ticket: TicketSummaryReadModel, force = false, append = false): void {
    if (!force && !append && this.conversations[ticket.ticketId]) {
      return;
    }

    const meta = this.conversationMeta[ticket.ticketId];
    if (append) {
      this.conversationLoadMoreId = ticket.ticketId;
    } else {
      this.conversationLoadingId = ticket.ticketId;
      delete this.conversationErrors[ticket.ticketId];
    }

    this.guildService
      .getTicketConversation(this.guildId, ticket.ticketId, {
        limit: 50,
        cursorOccurredAt: append ? meta?.nextCursorOccurredAt ?? undefined : undefined,
        cursorEventId: append ? meta?.nextCursorEventId ?? undefined : undefined
      })
      .subscribe({
        next: page => {
          const existing = append ? this.conversations[ticket.ticketId] ?? [] : [];
          this.conversations[ticket.ticketId] = append ? [...existing, ...page.items] : page.items;
          this.conversationMeta[ticket.ticketId] = {
            hasMore: page.hasMore,
            nextCursorOccurredAt: page.nextCursorOccurredAt,
            nextCursorEventId: page.nextCursorEventId
          };
          this.conversationLoadingId = '';
          this.conversationLoadMoreId = '';
        },
        error: err => {
          this.conversationLoadingId = '';
          this.conversationLoadMoreId = '';
          this.conversationErrors[ticket.ticketId] = getApiErrorMessage(
            err,
            this.translate.instant('tickets.conversation.loadError')
          );
        }
      });
  }

  conversationFor(ticket: TicketSummaryReadModel): TicketConversationEntryReadModel[] {
    return this.conversations[ticket.ticketId] ?? [];
  }

  conversationError(ticket: TicketSummaryReadModel): string {
    return this.conversationErrors[ticket.ticketId] ?? '';
  }

  hasMoreConversation(ticket: TicketSummaryReadModel): boolean {
    return this.conversationMeta[ticket.ticketId]?.hasMore ?? false;
  }

  get canReply(): boolean {
    return !!this.access?.canReplyToTickets;
  }

  get canClose(): boolean {
    return !!this.access?.canCloseTickets;
  }

  isOpen(ticket: TicketSummaryReadModel): boolean {
    return isTicketOpen(ticket.status);
  }

  isClosing(ticket: TicketSummaryReadModel): boolean {
    return this.closingTicketId === ticket.ticketId;
  }

  isReplying(ticket: TicketSummaryReadModel): boolean {
    return this.replyingTicketId === ticket.ticketId;
  }

  isSendingReply(ticket: TicketSummaryReadModel): boolean {
    return this.sendingReplyId === ticket.ticketId;
  }

  isConversationExpanded(ticket: TicketSummaryReadModel): boolean {
    return this.expandedConversationTicketId === ticket.ticketId;
  }

  isConversationLoading(ticket: TicketSummaryReadModel): boolean {
    return this.conversationLoadingId === ticket.ticketId;
  }

  isConversationLoadingMore(ticket: TicketSummaryReadModel): boolean {
    return this.conversationLoadMoreId === ticket.ticketId;
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

  statusLabel(status: number | string): string {
    return ticketStatusLabel(status);
  }

  displayMember(name?: string | null, id?: string | null): string {
    return displayMemberLabel(name, id);
  }

  displayChannel(channelId?: string | null): string {
    return displayChannelLabel(null, channelId);
  }
}
