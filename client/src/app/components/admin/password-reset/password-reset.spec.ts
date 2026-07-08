import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminPasswordResetComponent } from './password-reset';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('AdminPasswordResetComponent', () => {
  let component: AdminPasswordResetComponent;
  let fixture: ComponentFixture<AdminPasswordResetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminPasswordResetComponent],
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

    fixture = TestBed.createComponent(AdminPasswordResetComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
