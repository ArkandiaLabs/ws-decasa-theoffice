import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { Button } from '../button/button';

/**
 * Bloque de fallo con reintento. Nunca muestra el codigo HTTP: al comprador no le sirve un 500,
 * le sirve saber que puede volver a intentar.
 */
@Component({
  selector: 'app-error-state',
  imports: [Button],
  templateUrl: './error-state.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorState {
  readonly title = input('No pudimos cargar el catálogo');
  readonly message = input('Revisa tu conexión o inténtalo de nuevo en un momento.');
  readonly retryLabel = input('Reintentar');

  readonly retry = output<void>();
}
