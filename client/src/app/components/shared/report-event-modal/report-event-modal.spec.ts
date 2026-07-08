import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReportEventModalComponent } from './report-event-modal';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('ReportEventModalComponent', () => {
  let component: ReportEventModalComponent;
  let fixture: ComponentFixture<ReportEventModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReportEventModalComponent],
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

    fixture = TestBed.createComponent(ReportEventModalComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
