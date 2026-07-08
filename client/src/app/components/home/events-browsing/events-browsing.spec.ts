import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EventsBrowsingComponent } from './events-browsing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('EventsBrowsingComponent', () => {
  let component: EventsBrowsingComponent;
  let fixture: ComponentFixture<EventsBrowsingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventsBrowsingComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { queryParams: of({}), params: of({}) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EventsBrowsingComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
