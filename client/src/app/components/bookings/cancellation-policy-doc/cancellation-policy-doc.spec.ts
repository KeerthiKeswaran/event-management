import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CancellationPolicyDocComponent } from './cancellation-policy-doc';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('CancellationPolicyDocComponent', () => {
  let component: CancellationPolicyDocComponent;
  let fixture: ComponentFixture<CancellationPolicyDocComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CancellationPolicyDocComponent],
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

    fixture = TestBed.createComponent(CancellationPolicyDocComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
