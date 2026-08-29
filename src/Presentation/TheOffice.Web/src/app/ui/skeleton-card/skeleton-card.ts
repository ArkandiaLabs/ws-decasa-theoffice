import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Silueta de la tarjeta mientras carga el listado. Reproduce la altura de `ProductCard` para que
 * la grilla no salte cuando llegan los datos. Es decorativa: el lector de pantalla la ignora y
 * quien anuncia la carga es el contenedor.
 */
@Component({
  selector: 'app-skeleton-card',
  templateUrl: './skeleton-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonCard {}
