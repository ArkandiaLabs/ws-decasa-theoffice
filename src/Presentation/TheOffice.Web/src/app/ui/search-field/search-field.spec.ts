import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SearchField } from './search-field';

async function render(initial?: string) {
  const fixture: ComponentFixture<SearchField> = TestBed.createComponent(SearchField);
  if (initial !== undefined) {
    fixture.componentRef.setInput('value', initial);
  }
  await fixture.whenStable();

  const host = fixture.nativeElement as HTMLElement;
  const input = host.querySelector('input') as HTMLInputElement;
  const emitted: string[] = [];
  fixture.componentInstance.term.subscribe((value) => emitted.push(value));

  return { fixture, host, input, emitted };
}

function type(input: HTMLInputElement, value: string): void {
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

describe('SearchField', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('Type_SeveralKeystrokesWithinTheWindow_EmitsTermOnceWithTheLastValue', async () => {
    const { input, emitted } = await render();
    vi.useFakeTimers();

    type(input, 'r');
    type(input, 're');
    type(input, 'res');
    type(input, 'resma');
    vi.advanceTimersByTime(299);
    expect(emitted).toEqual([]);

    vi.advanceTimersByTime(1);

    expect(emitted).toEqual(['resma']);
  });

  it('Type_TwoBurstsSeparatedByThePause_EmitsTermTwice', async () => {
    const { input, emitted } = await render();
    vi.useFakeTimers();

    type(input, 'resma');
    vi.advanceTimersByTime(300);
    type(input, 'silla');
    vi.advanceTimersByTime(300);

    expect(emitted).toEqual(['resma', 'silla']);
  });

  it('Type_SameValueTwice_EmitsTermOnlyOnce', async () => {
    const { input, emitted } = await render();
    vi.useFakeTimers();

    type(input, 'resma');
    vi.advanceTimersByTime(300);
    type(input, 'resma');
    vi.advanceTimersByTime(300);

    expect(emitted).toEqual(['resma']);
  });

  it('Render_WithInitialValue_ShowsItInTheInput', async () => {
    const { input } = await render('resma');

    expect(input.value).toBe('resma');
    expect(input.type).toBe('search');
    expect(input.placeholder).toBe('Nombre o SKU (ej. PRD-001)');
  });

  it('Render_Always_HasAVisuallyHiddenLabelBoundToTheInput', async () => {
    const { host, input } = await render();
    const label = host.querySelector('label') as HTMLLabelElement;

    expect(label.textContent?.trim()).toBe('Buscar productos');
    expect(label.className).toContain('sr-only');
    expect(label.getAttribute('for')).toBe(input.id);
    expect(input.id).not.toBe('');
  });

  it('Render_TwoInstances_UseDifferentInputIds', async () => {
    const first = await render();
    const second = await render();

    expect(first.input.id).not.toBe(second.input.id);
  });
});
