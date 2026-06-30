import { Component, Input } from '@angular/core';
import { OnboardingChecklist } from '../../core/models/onboarding.models';

@Component({
  selector: 'app-onboarding-checklist',
  templateUrl: './onboarding-checklist.component.html',
  styleUrls: ['./onboarding-checklist.component.css']
})
export class OnboardingChecklistComponent {
  @Input() checklist: OnboardingChecklist | null = null;
  @Input() guildId = '';
  @Input() showProgress = true;
}
