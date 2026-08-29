import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { Button } from './button';

@Component({
  selector: 'app-button-host',
  imports: [Button],
  template: '<app-button>Reintentar</app-button>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class ButtonHost {}

async function render(inputs: Record<string, unknown> = {}) {
  const fixture = TestBed.createComponent(Button);
  for (const [name, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(name, value);
  }
  await fixture.whenStable();

  const host = fixture.nativeElement as HTMLElement;
  const button = host.querySelector('button') as HTMLButtonElement;

  return { fixture, button };
}

describe('Button', () => {
  it('Click_Enabled_EmitsPressedOnce', async () => {
    const { fixture, button } = await render();
    let emissions = 0;
    fixture.componentInstance.pressed.subscribe(() => (emissions += 1));

    button.click();

    expect(emissions).toBe(1);
  });

  it('Click_Disabled_DoesNotEmitPressed', async () => {
    const { fixture, button } = await render({ disabled: true });
    let emissions = 0;
    fixture.componentInstance.pressed.subscribe(() => (emissions += 1));

    button.click();
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(button.disabled).toBe(true);
    expect(emissions).toBe(0);
  });

  it('Render_Disabled_UsesDisabledTextColorAndNeverOpacity', async () => {
    const { button } = await render({ disabled: true });

    expect(button.className).toContain('text-text-muted');
    expect(button.className).toContain('cursor-not-allowed');
    expect(button.className).not.toContain('opacity-');
  });

  it('Render_PrimaryVariant_UsesThePrimaryBackground', async () => {
    const { button } = await render();

    expect(button.className).toContain('bg-primary');
    expect(button.className).toContain('min-h-11');
  });

  it('Render_TextVariant_HasNoBackgroundAndIsUnderlined', async () => {
    const { button } = await render({ variant: 'text' });

    expect(button.className).toContain('underline');
    expect(button.className).not.toContain('bg-');
  });

  it('Render_TypeSubmit_ReflectsTheTypeAttribute', async () => {
    const { button } = await render({ type: 'submit' });

    expect(button.type).toBe('submit');
  });

  it('Render_WithProjectedContent_ShowsTheLabel', async () => {
    const fixture = TestBed.createComponent(ButtonHost);
    await fixture.whenStable();

    const host = fixture.nativeElement as HTMLElement;
    const button = host.querySelector('button') as HTMLButtonElement;

    expect(button.textContent?.trim()).toBe('Reintentar');
  });
});
