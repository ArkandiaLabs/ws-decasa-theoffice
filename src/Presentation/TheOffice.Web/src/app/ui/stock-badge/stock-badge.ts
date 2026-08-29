import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Lo que la plantilla necesita para pintar un estado de stock: simbolo, palabra y color. */
interface StockState {
  readonly symbol: string;
  readonly label: string;
  readonly classes: string;
}

/**
 * Etiqueta de disponibilidad. La regla de negocio (>10 / 1..10 / 0) vive aqui y no en cada
 * pantalla, para que la tarjeta y el detalle no puedan discrepar.
 *
 * El color nunca comunica solo: siempre lo acompanan un simbolo decorativo y una palabra.
 */
@Component({
  selector: 'app-stock-badge',
  templateUrl: './stock-badge.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StockBadge {
  readonly stock = input.required<number>();

  protected readonly state = computed<StockState>(() => {
    const stock = this.stock();

    if (stock > 10) {
      return {
        symbol: '●',
        label: 'Disponible',
        classes: 'bg-stock-ok-bg text-stock-ok-fg border-stock-ok-border',
      };
    }

    if (stock >= 1) {
      return {
        symbol: '▲',
        label: 'Quedan pocas',
        classes: 'bg-stock-low-bg text-stock-low-fg border-stock-low-border',
      };
    }

    return {
      symbol: '✕',
      label: 'Agotado',
      classes: 'bg-stock-out-bg text-stock-out-fg border-stock-out-border',
    };
  });
}
