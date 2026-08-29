import { TestBed } from '@angular/core/testing';

import { SkeletonCard } from './skeleton-card';

describe('SkeletonCard', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SkeletonCard] }).compileComponents();
  });

  it('Render_Always_IsHiddenFromAssistiveTechnology', async () => {
    const fixture = TestBed.createComponent(SkeletonCard);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[aria-hidden="true"]')).toBeTruthy();
  });

  it('Render_Always_PulsesSkeletonBlocks', async () => {
    const fixture = TestBed.createComponent(SkeletonCard);
    await fixture.whenStable();

    const blocks = fixture.nativeElement.querySelectorAll('.bg-border.animate-pulse');
    expect(blocks.length).toBeGreaterThan(0);
  });

  it('Render_Always_HasNoReadableText', async () => {
    const fixture = TestBed.createComponent(SkeletonCard);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent.trim()).toBe('');
  });
});
