import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Etiqueta de producto descontinuado. Solo se usa en el detalle: el listado no trae el dato
 * de actividad, y una etiqueta que no se puede sostener con datos es peor que ninguna.
 *
 * Sin inputs a proposito: quien la muestra ya decidio que `isActive === false`.
 */
@Component({
  selector: 'app-discontinued-badge',
  templateUrl: './discontinued-badge.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiscontinuedBadge {}
