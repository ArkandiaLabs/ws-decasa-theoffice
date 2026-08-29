import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ProductListItem } from '../../catalog/catalog.models';
import { PricePipe } from '../../shared/price.pipe';
import { ProductImage } from '../product-image/product-image';
import { StockBadge } from '../stock-badge/stock-badge';

/**
 * Tarjeta de producto del listado. El SKU va arriba y en monoespaciada a proposito: es el dato
 * con el que el comprador arma el pedido, no letra chica.
 */
@Component({
  selector: 'app-product-card',
  imports: [RouterLink, PricePipe, ProductImage, StockBadge],
  templateUrl: './product-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCard {
  readonly product = input.required<ProductListItem>();
}
