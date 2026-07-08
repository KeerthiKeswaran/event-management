import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowseEventsComponent } from './browse-events';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('BrowseEventsComponent', () => {
  let component: BrowseEventsComponent;
  let fixture: ComponentFixture<BrowseEventsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrowseEventsComponent],
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

    fixture = TestBed.createComponent(BrowseEventsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
