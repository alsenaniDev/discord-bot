import { Component, Input } from '@angular/core';

export interface BreadcrumbItem {
  label: string;
  link?: string | any[];
  translate?: boolean;
}

@Component({
  selector: 'app-breadcrumbs',
  template: `
    <nav class="breadcrumbs" aria-label="Breadcrumb">
      <a *ngIf="homeLink" [routerLink]="homeLink">{{ 'nav.breadcrumbHome' | translate }}</a>
      <ng-container *ngFor="let item of items; let last = last">
        <span class="breadcrumb-sep" aria-hidden="true">/</span>
        <a *ngIf="item.link && !last" [routerLink]="item.link">
          {{ item.translate === false ? item.label : (item.label | translate) }}
        </a>
        <span *ngIf="!item.link || last" class="breadcrumb-current">
          {{ item.translate === false ? item.label : (item.label | translate) }}
        </span>
      </ng-container>
    </nav>
  `
})
export class BreadcrumbsComponent {
  @Input() items: BreadcrumbItem[] = [];
  @Input() homeLink: string | any[] | null = '/servers';
}
