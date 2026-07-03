import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import {
  TicketConversationEntryReadModel,
  TicketSummaryReadModel,
  displayMemberLabel,
  isTicketOpen,
  ticketDeliveryStatusLabel,
  ticketTimelineEventLabel
} from '../../../core/models/ticket.models';

@Component({
  selector: 'app-tickets-context-drawer',
  templateUrl: './tickets-context-drawer.component.html',
  styleUrls: ['./tickets-context-drawer.component.css']
})
export class TicketsContextDrawerComponent {
  @Input() ticket: TicketSummaryReadModel | null = null;
  @Input() guildId = '';
  @Input() open = false;
  @Input() inline = false;
  @Input() statusLabelKey = '';
  @Input() ownerLabel = '';
  @Input() channelLabel = '';
  @Input() canReply = false;
  @Input() canClose = false;
  @Input() isClosing = false;
  @Input() isReplying = false;
  @Input() isSendingReply = false;
  @Input() replyDraft = '';
  @Input() conversation: TicketConversationEntryReadModel[] = [];
  @Input() conversationLoading = false;
  @Input() conversationLoadingMore = false;
  @Input() conversationError = '';
  @Input() hasMoreConversation = false;

  @Output() closeDrawer = new EventEmitter<void>();
  @Output() refreshConversation = new EventEmitter<void>();
  @Output() loadMoreConversation = new EventEmitter<void>();
  @Output() toggleReply = new EventEmitter<void>();
  @Output() replyDraftChange = new EventEmitter<string>();
  @Output() sendReply = new EventEmitter<void>();
  @Output() closeTicket = new EventEmitter<void>();

  @ViewChild('detailTitle') detailTitle?: ElementRef<HTMLElement>;

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeDrawer.emit();
    }
  }

  onReplyDraftInput(value: string): void {
    this.replyDraftChange.emit(value);
  }

  focusTitle(): void {
    requestAnimationFrame(() => {
      this.detailTitle?.nativeElement.focus({ preventScroll: true });
    });
  }

  get ticketIsOpen(): boolean {
    return !!this.ticket && isTicketOpen(this.ticket.status);
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
