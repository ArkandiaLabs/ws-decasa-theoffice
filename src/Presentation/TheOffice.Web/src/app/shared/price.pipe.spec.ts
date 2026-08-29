import { TestBed } from '@angular/core/testing';

import { appConfig } from '../app.config';
import { PricePipe } from './price.pipe';

// Se usan los providers reales de la aplicacion a proposito: la prueba tiene que fallar si
// alguien quita el LOCALE_ID o el registerLocaleData de app.config.ts. Con un LOCALE_ID
// inventado aqui, ese bug solo aparece en pantalla.
describe('PricePipe', () => {
  let pipe: PricePipe;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [...appConfig.providers, PricePipe] });
    pipe = TestBed.inject(PricePipe);
  });

  it('Transform_ThousandsValue_GroupsWithDots', () => {
    expect(pipe.transform(18900)).toBe('$ 18.900');
  });

  it('Transform_MillionsValue_GroupsEveryThreeDigits', () => {
    expect(pipe.transform(1250000)).toBe('$ 1.250.000');
  });

  it('Transform_SmallestSeededPrice_KeepsNoDecimals', () => {
    expect(pipe.transform(9800)).toBe('$ 9.800');
  });

  it('Transform_Zero_RendersZero', () => {
    expect(pipe.transform(0)).toBe('$ 0');
  });
});
