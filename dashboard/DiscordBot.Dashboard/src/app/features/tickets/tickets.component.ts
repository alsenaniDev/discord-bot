import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GuildService } from '../../core/services/guild.service';
import { GuildContextService } from '../../core/services/guild-context.service';
import { ToastService } from '../../core/services/toast.service';
import { Ticket, ticketStatusLabel } from '../../core/models/ticket.models';
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

  statusLabel(status: number | string): string {
    return ticketStatusLabel(status);
  }
}
