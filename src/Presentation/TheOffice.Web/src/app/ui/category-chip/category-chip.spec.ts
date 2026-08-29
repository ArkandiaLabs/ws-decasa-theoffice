import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CategoryChip } from './category-chip';

async function render(inputs: Record<string, unknown>) {
  const fixture = TestBed.createComponent(CategoryChip);
  for (const [name, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(name, value);
  }
  await fixture.whenStable();

  return { fixture, host: fixture.nativeElement as HTMLElement };
}

describe('CategoryChip', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('Click_InactiveFilterChip_EmitsPicked', async () => {
    const { fixture, host } = await render({ label: 'Papelería' });
    const emissions: string[] = [];
    fixture.componentInstance.picked.subscribe(() => emissions.push('picked'));
    fixture.componentInstance.cleared.subscribe(() => emissions.push('cleared'));

    (host.querySelector('button') as HTMLButtonElement).click();

    expect(emissions).toEqual(['picked']);
  });

  it('Click_ActiveFilterChip_EmitsCleared', async () => {
    const { fixture, host } = await render({ label: 'Papelería', active: true });
    const emissions: string[] = [];
    fixture.componentInstance.picked.subscribe(() => emissions.push('picked'));
    fixture.componentInstance.cleared.subscribe(() => emissions.push('cleared'));

    const remove = host.querySelector('[aria-label="Quitar filtro de Papelería"]');
    (remove as HTMLButtonElement).click();

    expect(emissions).toEqual(['cleared']);
  });

  it('Render_ActiveFilterChip_ShowsTheRemoveMarkAndItsAccessibleName', async () => {
    const { host } = await render({ label: 'Mobiliario', active: true });
    const button = host.querySelector('button') as HTMLButtonElement;

    expect(button.getAttribute('aria-label')).toBe('Quitar filtro de Mobiliario');
    expect(button.textContent).toContain('✕');
    expect(button.className).toContain('bg-foreground');
  });

  it('Render_InactiveFilterChip_HasNoRemoveMarkAndNoAriaLabel', async () => {
    const { host } = await render({ label: 'Mobiliario' });
    const button = host.querySelector('button') as HTMLButtonElement;

    expect(button.hasAttribute('aria-label')).toBe(false);
    expect(button.textContent).not.toContain('✕');
    expect(button.textContent?.trim()).toBe('Mobiliario');
    expect(button.className).toContain('min-h-11');
  });

  it('Render_LinkVariant_IsAnAnchorToProductsWithTheQueryParams', async () => {
    const { host } = await render({
      label: 'Papelería',
      variant: 'link',
      linkParams: { categoria: 'papeleria' },
    });

    const anchor = host.querySelector('a') as HTMLAnchorElement;
    expect(host.querySelector('button')).toBeNull();
    expect(anchor.textContent?.trim()).toBe('Papelería');
    expect(anchor.getAttribute('href')).toBe('/productos?categoria=papeleria');
  });

  it('Render_LinkVariantWithoutParams_StillLinksToProducts', async () => {
    const { host } = await render({ label: 'Todas', variant: 'link' });

    const anchor = host.querySelector('a') as HTMLAnchorElement;
    expect(anchor.getAttribute('href')).toBe('/productos');
  });
  // El chip "Todas" es el estado sin filtro: marcarlo como quitable ofrece una accion que no
  // hace nada, y el lector de pantalla la anuncia como si la hiciera.
  it('Render_ActiveButNotClearable_HidesTheRemoveAffordance', async () => {
    const fixture = TestBed.createComponent(CategoryChip);
    fixture.componentRef.setInput('label', 'Todas');
    fixture.componentRef.setInput('active', true);
    fixture.componentRef.setInput('clearable', false);
    await fixture.whenStable();
    const button = fixture.nativeElement.querySelector('button');

    expect(button.textContent).not.toContain('✕');
    expect(button.getAttribute('aria-label')).toBeNull();
  });

  it('Click_ActiveButNotClearable_EmitsNothing', async () => {
    const fixture = TestBed.createComponent(CategoryChip);
    fixture.componentRef.setInput('label', 'Todas');
    fixture.componentRef.setInput('active', true);
    fixture.componentRef.setInput('clearable', false);
    await fixture.whenStable();
    let emitted = 0;
    fixture.componentInstance.cleared.subscribe(() => (emitted += 1));
    fixture.componentInstance.picked.subscribe(() => (emitted += 1));

    fixture.nativeElement.querySelector('button').click();
    await fixture.whenStable();

    expect(emitted).toBe(0);
  });
});
