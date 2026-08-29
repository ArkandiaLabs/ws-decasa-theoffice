import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmptyState } from './empty-state';

describe('EmptyState', () => {
  let fixture: ComponentFixture<EmptyState>;

  function buttonAt(testId: string): HTMLButtonElement | null {
    return fixture.nativeElement.querySelector(`[data-testid="${testId}"] button`);
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [EmptyState] }).compileComponents();

    fixture = TestBed.createComponent(EmptyState);
    fixture.componentRef.setInput('title', 'Sin resultados');
    fixture.componentRef.setInput('message', 'No encontramos productos con ese criterio.');
  });

  it('Render_Always_ShowsTitleAndMessage', async () => {
    await fixture.whenStable();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Sin resultados');
    expect(text).toContain('No encontramos productos con ese criterio.');
  });

  it('Render_WithoutPrimaryLabel_OmitsThePrimaryButton', async () => {
    await fixture.whenStable();

    expect(buttonAt('primary-button')).toBeNull();
  });

  it('Render_WithPrimaryLabel_ShowsThePrimaryButton', async () => {
    fixture.componentRef.setInput('primaryLabel', 'Limpiar filtros');
    await fixture.whenStable();

    expect(buttonAt('primary-button')?.textContent?.trim()).toBe('Limpiar filtros');
  });

  it('Click_PrimaryButton_EmitsPrimary', async () => {
    fixture.componentRef.setInput('primaryLabel', 'Limpiar filtros');
    await fixture.whenStable();

    let emitted = 0;
    fixture.componentInstance.primary.subscribe(() => emitted++);

    buttonAt('primary-button')?.click();
    await fixture.whenStable();

    expect(emitted).toBe(1);
  });

  it('Render_WithoutSecondaryLabel_OmitsTheSecondaryButton', async () => {
    fixture.componentRef.setInput('primaryLabel', 'Limpiar filtros');
    await fixture.whenStable();

    expect(buttonAt('secondary-button')).toBeNull();
  });

  it('Click_SecondaryButton_EmitsSecondary', async () => {
    fixture.componentRef.setInput('secondaryLabel', 'Ver todo el catálogo');
    await fixture.whenStable();

    let emitted = 0;
    fixture.componentInstance.secondary.subscribe(() => emitted++);

    buttonAt('secondary-button')?.click();
    await fixture.whenStable();

    expect(emitted).toBe(1);
  });

  it('Render_WithHint_ShowsTheHint', async () => {
    fixture.componentRef.setInput('hint', 'Prueba con el SKU completo.');
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Prueba con el SKU completo.');
  });
});
