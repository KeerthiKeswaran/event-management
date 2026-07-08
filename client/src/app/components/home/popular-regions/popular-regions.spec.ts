import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PopularRegionsComponent } from './popular-regions';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('PopularRegionsComponent', () => {
  let component: PopularRegionsComponent;
  let fixture: ComponentFixture<PopularRegionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PopularRegionsComponent],
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

    fixture = TestBed.createComponent(PopularRegionsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
