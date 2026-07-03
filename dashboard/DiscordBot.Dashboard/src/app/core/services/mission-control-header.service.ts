import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { MissionControlHeaderState, StatusStripModel } from '../models/mission-control.models';

const INITIAL_STATE: MissionControlHeaderState = {
  visible: false,
  loading: false,
  model: null
};

@Injectable({ providedIn: 'root' })
export class MissionControlHeaderService {
  private readonly stateSubject = new BehaviorSubject<MissionControlHeaderState>(INITIAL_STATE);
  readonly state$ = this.stateSubject.asObservable();

  showLoading(): void {
    this.stateSubject.next({
      visible: true,
      loading: true,
      model: null
    });
  }

  setStatus(model: StatusStripModel): void {
    this.stateSubject.next({
      visible: true,
      loading: false,
      model
    });
  }

  clear(): void {
    this.stateSubject.next(INITIAL_STATE);
  }
}
