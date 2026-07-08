import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AboutFaqComponent } from './about-faq';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('AboutFaqComponent', () => {
  let component: AboutFaqComponent;
  let fixture: ComponentFixture<AboutFaqComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AboutFaqComponent],
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

    fixture = TestBed.createComponent(AboutFaqComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
