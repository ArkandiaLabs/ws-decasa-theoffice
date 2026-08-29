import { formatNumber } from '@angular/common';
import { inject, LOCALE_ID, Pipe, PipeTransform } from '@angular/core';

/**
 * Precio en pesos colombianos: `$ 18.900`. Sin decimales y con punto de miles, que es como
 * se escribe un precio en Colombia. No se usa `CurrencyPipe` porque su simbolo y su
 * separacion cambian con la version de los datos de CLDR, y aqui el formato es del diseno.
 */
@Pipe({ name: 'price' })
export class PricePipe implements PipeTransform {
  private readonly locale = inject(LOCALE_ID);

  transform(value: number): string {
    return `$ ${formatNumber(value, this.locale, '1.0-0')}`;
  }
}
