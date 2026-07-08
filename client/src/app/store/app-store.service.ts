import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, distinctUntilChanged } from 'rxjs/operators';
import { AppState, initialAppState } from './state/app.state';
import { appReducer } from './reducers/app.reducer';

@Injectable({
  providedIn: 'root'
})
export class AppStoreService {
  private readonly stateSubject = new BehaviorSubject<AppState>(initialAppState);
  public readonly state$: Observable<AppState> = this.stateSubject.asObservable();

  constructor() {}

  // Get current state snapshot
  public get state(): AppState {
    return this.stateSubject.getValue();
  }

  // Dispatch an action to mutate state
  public dispatch(action: { type: string; payload?: any }): void {
    const currentState = this.state;
    const newState = appReducer(currentState, action);
    this.stateSubject.next(newState);
  }

  // Select a slice of state using a selector function
  public select<T>(selector: (state: AppState) => T): Observable<T> {
    return this.state$.pipe(
      map(selector),
      distinctUntilChanged()
    );
  }
}
