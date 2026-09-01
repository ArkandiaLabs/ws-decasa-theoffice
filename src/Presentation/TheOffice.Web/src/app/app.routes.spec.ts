import { registerLocaleData } from '@angular/common';
import localeEsCo from '@angular/common/locales/es-CO';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { of } from 'rxjs';

import { App } from './app';
import { routes } from './app.routes';
import {
  Category,
  Fetched,
  PagedResult,
  ProductDetail,
  ProductListItem,
} from './catalog/catalog.models';
import { CatalogService } from './catalog/catalog.service';

// El listado y la ficha pintan precios; sin los datos del locale el pipe cae a en-US.
registerLocaleData(localeEsCo);

const emptyPage: Fetched<PagedResult<ProductListItem>> = {
  kind: 'ok',
  value: { items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 },
};

const detail: Fetched<ProductDetail> = {
  kind: 'ok',
  value: {
    publicId: 'PRD-001',
    name: 'Resma de papel carta 75 g',
    description: 'Papel bond de 75 gramos.',
    price: 18900,
    imageUrl: '',
    stock: 42,
    isActive: true,
    category: null,
  },
};

interface Mounted {
  readonly fixture: ComponentFixture<App>;
  readonly host: HTMLElement;
  readonly router: Router;
  readonly getProduct: ReturnType<typeof vi.fn>;
}

/**
 * Monta el shell real con las rutas reales. Es lo unico que ejercita `app.routes.ts`: la carga
 * diferida de cada pantalla, el comodin, y `withComponentInputBinding()`, que es lo que hace que
 * el SKU de la URL llegue a la ficha como `input`. Sin esto, cambiar el nombre del parametro de
 * ruta rompe el detalle y ninguna prueba lo dice.
 */
async function mount(url: string): Promise<Mounted> {
  const getProduct = vi.fn(() => of(detail));
  const catalog = {
    getProducts: vi.fn(() => of(emptyPage)),
    getProduct,
    getCategories: vi.fn(() => of({ kind: 'ok', value: [] as readonly Category[] })),
  };

  TestBed.configureTestingModule({
    providers: [
      provideRouter(routes, withComponentInputBinding()),
      { provide: LOCALE_ID, useValue: 'es-CO' },
      { provide: CatalogService, useValue: catalog },
    ],
  });

  const router = TestBed.inject(Router);
  const fixture = TestBed.createComponent(App);
  await router.navigateByUrl(url);
  await fixture.whenStable();

  return { fixture, host: fixture.nativeElement as HTMLElement, router, getProduct };
}

describe('routes', () => {
  it('Navigate_TheRoot_MountsTheListing', async () => {
    const { host } = await mount('/');

    expect(host.querySelector('app-product-list')).toBeTruthy();
    expect(host.querySelector('app-product-detail')).toBeNull();
  });

  // El listado vive en dos rutas: la miga de pan y el enlace de vuelta necesitan una URL propia.
  it('Navigate_TheNamedListingPath_MountsTheSameScreen', async () => {
    const { host, router } = await mount('/productos');

    expect(router.url).toBe('/productos');
    expect(host.querySelector('app-product-list')).toBeTruthy();
  });

  it('Navigate_ASkuPath_MountsTheDetailAndBindsTheSkuAsAnInput', async () => {
    const { host, getProduct } = await mount('/productos/PRD-001');

    expect(host.querySelector('app-product-detail')).toBeTruthy();
    expect(host.querySelector('app-product-list')).toBeNull();
    expect(getProduct).toHaveBeenCalledWith('PRD-001');
  });

  // El comodin manda a la raiz: una URL inventada no muestra una pantalla en blanco.
  it('Navigate_AnUnknownPath_RedirectsToTheListing', async () => {
    const { host, router } = await mount('/lo-que-sea');

    expect(router.url).toBe('/');
    expect(host.querySelector('app-product-list')).toBeTruthy();
  });

  it('Navigate_AnUnknownNestedPath_AlsoRedirectsToTheListing', async () => {
    const { router } = await mount('/productos/PRD-001/comprar');

    expect(router.url).toBe('/');
  });

  // La marca vive en el shell, no en la pantalla: existe en las dos rutas. Una ficha sin
  // cabecera parece una pagina rota, y el enlace de vuelta a la raiz tiene que estar siempre.
  it('Navigate_TheListing_KeepsTheBrandHeader', async () => {
    const { host } = await mount('/');

    expect(host.querySelector('header a')?.textContent?.trim()).toBe('TheOffice');
  });

  it('Navigate_TheDetail_KeepsTheBrandHeader', async () => {
    const { host } = await mount('/productos/PRD-001');

    expect(host.querySelector('header a')?.textContent?.trim()).toBe('TheOffice');
  });

  it('Navigate_TheListing_SetsTheDocumentTitle', async () => {
    await mount('/productos');

    expect(document.title).toBe('TheOffice — Catálogo');
  });
});
