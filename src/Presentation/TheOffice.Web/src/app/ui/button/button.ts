import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

type ButtonVariant = 'primary' | 'secondary' | 'text';

const BASE = 'inline-flex min-h-11 items-center justify-center rounded-sm px-4 text-label';

const ENABLED: Record<ButtonVariant, string> = {
  primary: 'bg-primary text-on-primary hover:bg-primary-hover active:bg-primary-active',
  secondary: 'bg-neutral text-foreground border border-secondary hover:bg-border',
  text: 'text-tertiary-strong underline underline-offset-2 hover:text-tertiary-strong',
};

/**
 * Deshabilitado se comunica con color de texto y cursor, nunca con `opacity-*`: la opacidad
 * atenua tambien el fondo y deja el contraste por debajo del minimo AA.
 */
const DISABLED: Record<ButtonVariant, string> = {
  primary: 'cursor-not-allowed border border-border bg-neutral text-text-muted',
  secondary: 'cursor-not-allowed border border-border bg-neutral text-text-muted',
  text: 'cursor-not-allowed text-text-muted',
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
