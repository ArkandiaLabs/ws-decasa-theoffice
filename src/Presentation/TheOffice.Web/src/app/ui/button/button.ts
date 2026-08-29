import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

type ButtonVariant = 'primary' | 'secondary' | 'text';

const BASE = 'inline-flex min-h-11 items-center justify-center rounded-md px-4 text-ui';

const ENABLED: Record<ButtonVariant, string> = {
  primary: 'bg-primary-700 text-surface hover:bg-primary-500',
  secondary: 'bg-surface-muted text-text-body border border-border-strong hover:bg-primary-100',
  text: 'text-primary-700 underline underline-offset-2 hover:text-primary-500',
};

/**
 * Deshabilitado se comunica con color de texto y cursor, nunca con `opacity-*`: la opacidad
 * atenua tambien el fondo y deja el contraste por debajo del minimo AA.
 */
const DISABLED: Record<ButtonVariant, string> = {
  primary: 'cursor-not-allowed border border-border bg-surface-muted text-text-disabled',
  secondary: 'cursor-not-allowed border border-border bg-surface-muted text-text-disabled',
  text: 'cursor-not-allowed text-text-disabled',
};

@Component({
  selector: 'app-button',
  templateUrl: './button.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Button {
  readonly variant = input<ButtonVariant>('primary');
  readonly disabled = input(false);
  readonly type = input<'button' | 'submit'>('button');
  readonly pressed = output<void>();

  protected readonly classes = computed(() =>
    this.disabled() ? `${BASE} ${DISABLED[this.variant()]}` : `${BASE} ${ENABLED[this.variant()]}`,
  );

  protected onClick(): void {
    // El atributo `disabled` ya frena el click del navegador; esta guarda cubre el click
    // sintetico (pruebas, scripts) para que el contrato "no emite" valga siempre.
    if (this.disabled()) {
      return;
    }

    this.pressed.emit();
  }
}
