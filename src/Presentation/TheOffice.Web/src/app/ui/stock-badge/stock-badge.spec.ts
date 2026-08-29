import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StockBadge } from './stock-badge';

describe('StockBadge', () => {
  let fixture: ComponentFixture<StockBadge>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [StockBadge] }).compileComponents();
    fixture = TestBed.createComponent(StockBadge);
  });

  async function render(stock: number): Promise<HTMLElement> {
    fixture.componentRef.setInput('stock', stock);
    await fixture.whenStable();

    return fixture.nativeElement.querySelector('span') as HTMLElement;
  }

  function text(badge: HTMLElement): string {
    return (badge.textContent ?? '').replace(/\s+/g, ' ').trim();
  }

  it('Render_StockAboveTen_ShowsAvailable', async () => {
    const badge = await render(42);

    expect(text(badge)).toBe('● Disponible');
    expect(badge.classList).toContain('bg-stock-ok-bg');
    expect(badge.classList).toContain('text-stock-ok-fg');
    expect(badge.classList).toContain('border-stock-ok-border');
  });

  it('Render_StockEleven_ShowsAvailableAtTheLowerBoundary', async () => {
    const badge = await render(11);

    expect(text(badge)).toBe('● Disponible');
    expect(badge.classList).toContain('bg-stock-ok-bg');
  });

  it('Render_StockTen_ShowsRunningLowAtTheUpperBoundary', async () => {
    const badge = await render(10);

    expect(text(badge)).toBe('▲ Quedan pocas');
    expect(badge.classList).toContain('bg-stock-low-bg');
    expect(badge.classList).toContain('text-stock-low-fg');
    expect(badge.classList).toContain('border-stock-low-border');
  });

  it('Render_StockOne_ShowsRunningLowAtTheLowerBoundary', async () => {
    const badge = await render(1);

    expect(text(badge)).toBe('▲ Quedan pocas');
    expect(badge.classList).toContain('bg-stock-low-bg');
  });

  it('Render_StockZero_ShowsOutOfStock', async () => {
    const badge = await render(0);

    expect(text(badge)).toBe('✕ Agotado');
    expect(badge.classList).toContain('bg-stock-out-bg');
    expect(badge.classList).toContain('text-stock-out-fg');
    expect(badge.classList).toContain('border-stock-out-border');
  });

  it('Render_AnyStock_MarksTheSymbolAsDecorative', async () => {
    const badge = await render(0);
    const symbol = badge.querySelector('span');

    expect(symbol?.getAttribute('aria-hidden')).toBe('true');
    expect(symbol?.textContent?.trim()).toBe('✕');
  });

  it('Render_AnyStock_KeepsTheSharedBadgeShape', async () => {
    const badge = await render(5);

    for (const shapeClass of ['inline-flex', 'items-center', 'gap-1', 'rounded-sm', 'border']) {
      expect(badge.classList).toContain(shapeClass);
    }
  });

  it('Render_StockChangesFromZeroToAvailable_SwapsSymbolTextAndClasses', async () => {
    const outOfStock = await render(0);
    expect(text(outOfStock)).toBe('✕ Agotado');

    const available = await render(11);
    expect(text(available)).toBe('● Disponible');
    expect(available.classList).toContain('bg-stock-ok-bg');
    expect(available.classList).not.toContain('bg-stock-out-bg');
  });
});
