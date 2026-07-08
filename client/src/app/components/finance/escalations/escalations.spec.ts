import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EscalationsComponent } from './escalations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('EscalationsComponent', () => {
  let component: EscalationsComponent;
  let fixture: ComponentFixture<EscalationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EscalationsComponent],
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

    fixture = TestBed.createComponent(EscalationsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
