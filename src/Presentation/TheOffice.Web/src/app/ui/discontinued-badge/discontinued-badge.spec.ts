import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DiscontinuedBadge } from './discontinued-badge';

describe('DiscontinuedBadge', () => {
  let fixture: ComponentFixture<DiscontinuedBadge>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [DiscontinuedBadge] }).compileComponents();
    fixture = TestBed.createComponent(DiscontinuedBadge);
    await fixture.whenStable();
  });

  function badge(): HTMLElement {
    return fixture.nativeElement.querySelector('span') as HTMLElement;
  }

  it('Render_Always_ShowsTheDiscontinuedSymbolAndWord', () => {
    expect((badge().textContent ?? '').replace(/\s+/g, ' ').trim()).toBe('◼ Descontinuado');
  });

  it('Render_Always_MarksTheSymbolAsDecorative', () => {
    const symbol = badge().querySelector('span');

    expect(symbol?.getAttribute('aria-hidden')).toBe('true');
    expect(symbol?.textContent?.trim()).toBe('◼');
  });

  it('Render_Always_UsesTheDiscontinuedTokens', () => {
    expect(badge().classList).toContain('bg-discontinued-bg');
    expect(badge().classList).toContain('text-discontinued-fg');
    expect(badge().classList).toContain('border-discontinued-border');
  });

  it('Render_Always_KeepsTheSharedBadgeShape', () => {
    for (const shapeClass of ['inline-flex', 'items-center', 'gap-1', 'rounded-sm', 'border']) {
      expect(badge().classList).toContain(shapeClass);
    }
  });
});
