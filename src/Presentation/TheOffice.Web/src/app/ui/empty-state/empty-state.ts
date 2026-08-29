import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { Button } from '../button/button';

/**
 * Bloque para "no hay nada que mostrar". No es un error: no se pinta en rojo y siempre ofrece
 * una salida. Una etiqueta nula significa que ese boton no existe en este caso.
 */
@Component({
  selector: 'app-empty-state',
  imports: [Button],
  templateUrl: './empty-state.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly primaryLabel = input<string | null>(null);
  readonly secondaryLabel = input<string | null>(null);

  readonly primary = output<void>();
  readonly secondary = output<void>();
}
