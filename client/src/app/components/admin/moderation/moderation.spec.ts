import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminModerationComponent } from './moderation';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('AdminModerationComponent', () => {
  let component: AdminModerationComponent;
  let fixture: ComponentFixture<AdminModerationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminModerationComponent],
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

    fixture = TestBed.createComponent(AdminModerationComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
