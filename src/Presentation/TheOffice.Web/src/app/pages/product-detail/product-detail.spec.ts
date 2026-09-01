import { registerLocaleData } from '@angular/common';
import localeEsCo from '@angular/common/locales/es-CO';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { Observable, of } from 'rxjs';

import { Fetched, ProductDetail } from '../../catalog/catalog.models';
import { CatalogService } from '../../catalog/catalog.service';
import { ProductDetailPage } from './product-detail';

// Sin los datos del locale, `formatNumber` cae a en-US y el precio se probaria contra un
// formato que el comprador nunca ve.
registerLocaleData(localeEsCo);

const product: ProductDetail = {
  publicId: 'PRD-001',
  name: 'Resma de papel carta 75 g',
  description: 'Papel bond de 75 gramos, resma por 500 hojas, tamaño carta.',
  price: 18900,
  images: [
    { publicId: 'PRD-001-IMG-1', url: '/img/prd-001-1.jpg', sortOrder: 0, isPrimary: true },
    { publicId: 'PRD-001-IMG-2', url: '/img/prd-001-2.jpg', sortOrder: 1, isPrimary: false },
    { publicId: 'PRD-001-IMG-3', url: '/img/prd-001-3.jpg', sortOrder: 2, isPrimary: false },
  ],
  stock: 42,
  isActive: true,
  category: {
    publicId: 'CAT-001',
    name: 'Papeleria',
    slug: 'papeleria',
    description: 'Papel, cuadernos y utiles de escritorio',
  },
};

const getProduct = vi.fn<(publicId: string) => Observable<Fetched<ProductDetail>>>();
const writeText = vi.fn<(text: string) => Promise<void>>();

function dom(fixture: ComponentFixture<ProductDetailPage>): HTMLElement {
  return fixture.nativeElement as HTMLElement;
}

describe('ProductDetailPage', () => {
  async function render(
    result: Fetched<ProductDetail>,
    queryParams: Record<string, string> = {},
    publicId = 'PRD-001',
  ): Promise<ComponentFixture<ProductDetailPage>> {
    getProduct.mockReturnValue(of(result));

    await TestBed.configureTestingModule({
      imports: [ProductDetailPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'es-CO' },
        { provide: CatalogService, useValue: { getProduct } },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: of(convertToParamMap(queryParams)) },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductDetailPage);
    fixture.componentRef.setInput('publicId', publicId);
    await fixture.whenStable();

    return fixture;
  }

  beforeEach(() => {
    getProduct.mockReset();
    writeText.mockReset();
    writeText.mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
  });

  it('Render_ProductWithCategory_LinksTheChipToTheFilteredListing', async () => {
    const fixture = await render({ kind: 'ok', value: product });

    const chip = dom(fixture).querySelector('app-category-chip a');
    expect(chip?.getAttribute('href')).toBe('/productos?category=papeleria');
    expect(chip?.textContent?.trim()).toBe('Papeleria');
  });

  it('Render_ProductWithoutCategory_HidesTheChipAndCollapsesTheBreadcrumb', async () => {
    const fixture = await render({ kind: 'ok', value: { ...product, category: null } });

    expect(dom(fixture).querySelector('app-category-chip')).toBeNull();
    expect(dom(fixture).querySelectorAll('[data-testid="breadcrumb"] li').length).toBe(2);
    expect(dom(fixture).querySelector('[data-testid="spec-category"]')?.textContent?.trim()).toBe(
      'Sin categoría asignada',
    );
  });

  it('Render_ProductWithCategory_ShowsTheThreeBreadcrumbLevels', async () => {
    const fixture = await render({ kind: 'ok', value: product });

    expect(dom(fixture).querySelectorAll('[data-testid="breadcrumb"] li').length).toBe(3);
  });

  it('Render_InactiveProduct_ShowsTheNoticeWithoutHidingTheProduct', async () => {
    const fixture = await render({ kind: 'ok', value: { ...product, isActive: false } });

    expect(dom(fixture).querySelector('[data-testid="discontinued-notice"]')).toBeTruthy();
    expect(dom(fixture).querySelector('app-discontinued-badge')).toBeTruthy();
    expect(dom(fixture).textContent).toContain('Resma de papel carta 75 g');
  });

  it('Render_OutOfStockProduct_ShowsTheNoticeBesidesTheBadge', async () => {
    const fixture = await render({ kind: 'ok', value: { ...product, stock: 0 } });

    expect(dom(fixture).querySelector('[data-testid="out-of-stock-notice"]')).toBeTruthy();
    expect(dom(fixture).querySelector('app-stock-badge')?.textContent).toContain('Agotado');
  });

  it('Render_NotFound_NamesTheRequestedSku', async () => {
    const fixture = await render({ kind: 'not-found' }, {}, 'PRD-042');

    const text = dom(fixture).textContent ?? '';
    expect(dom(fixture).querySelector('app-empty-state')).toBeTruthy();
    expect(text).toContain('No encontramos el producto PRD-042');
    expect(text).toContain('PRD-001');
  });

  it('Render_Error_ShowsTheErrorStateAndRetryFetchesAgain', async () => {
    const fixture = await render({ kind: 'error' });

    expect(dom(fixture).querySelector('app-error-state')).toBeTruthy();
    expect(getProduct).toHaveBeenCalledTimes(1);

    dom(fixture).querySelector<HTMLButtonElement>('[data-testid="retry-button"] button')?.click();
    await fixture.whenStable();

    expect(getProduct).toHaveBeenCalledTimes(2);
  });

  it('Render_Loading_ShowsTheSkeletonAndAnnouncesIt', async () => {
    getProduct.mockReturnValue(new Observable<Fetched<ProductDetail>>());

    await TestBed.configureTestingModule({
      imports: [ProductDetailPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'es-CO' },
        { provide: CatalogService, useValue: { getProduct } },
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap({})) } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductDetailPage);
    fixture.componentRef.setInput('publicId', 'PRD-001');
    await fixture.whenStable();

    expect(dom(fixture).querySelector('.animate-pulse')).toBeTruthy();
    expect(dom(fixture).querySelector('[role="status"]')?.textContent).toContain(
      'Cargando producto',
    );
  });

  it('Render_WithListQueryParams_KeepsThemOnTheBackLink', async () => {
    const fixture = await render(
      { kind: 'ok', value: product },
      {
        page: '2',
        category: 'papeleria',
        search: 'resma',
      },
    );

    const href = dom(fixture).querySelector('[data-testid="back-link"]')?.getAttribute('href');
    expect(href).toContain('/productos?');
    expect(href).toContain('page=2');
    expect(href).toContain('category=papeleria');
    expect(href).toContain('search=resma');
  });

  it('Render_WithoutQueryParams_SendsTheBackLinkToTheCleanListing', async () => {
    const fixture = await render({ kind: 'ok', value: product });

    expect(dom(fixture).querySelector('[data-testid="back-link"]')?.getAttribute('href')).toBe(
      '/productos',
    );
  });

  it('Copy_ClipboardAvailable_CopiesTheSkuAndConfirmsIt', async () => {
    const fixture = await render({ kind: 'ok', value: product });

    dom(fixture).querySelector<HTMLButtonElement>('[data-testid="copy-button"] button')?.click();
    await fixture.whenStable();
    await fixture.whenStable();

    expect(writeText).toHaveBeenCalledWith('PRD-001');
    expect(dom(fixture).querySelector('[data-testid="copy-status"]')?.textContent).toContain(
      'Copiado',
    );
  });

  it('Copy_ClipboardRejects_KeepsQuiet', async () => {
    writeText.mockRejectedValue(new Error('sin permiso'));
    const fixture = await render({ kind: 'ok', value: product });

    dom(fixture).querySelector<HTMLButtonElement>('[data-testid="copy-button"] button')?.click();
    await fixture.whenStable();
    await fixture.whenStable();

    expect(dom(fixture).querySelector('[data-testid="copy-status"]')?.textContent?.trim()).toBe('');
  });

  it('Render_ProductWithSeveralPhotos_HandsTheWholeGalleryToTheComponent', async () => {
    const fixture = await render({ kind: 'ok', value: product });

    expect(dom(fixture).querySelectorAll('app-product-gallery [role="tab"]').length).toBe(3);
    expect(
      dom(fixture).querySelector('app-product-gallery img[loading="eager"]')?.getAttribute('src'),
    ).toBe('/img/prd-001-1.jpg');
  });

  // El catalogo tiene referencias con una sola toma, y ninguna es un error que haya que avisar.
  it('Render_ProductWithoutPhotos_StillShowsTheProduct', async () => {
    const fixture = await render({ kind: 'ok', value: { ...product, images: [] } });

    expect(dom(fixture).querySelector('app-product-gallery')).toBeTruthy();
    expect(dom(fixture).textContent).toContain('Sin imagen');
    expect(dom(fixture).textContent).toContain('Resma de papel carta 75 g');
  });

  it('Render_Always_HasNoPurchaseCallToAction', async () => {
    const fixture = await render({ kind: 'ok', value: { ...product, stock: 0, isActive: false } });

    expect(dom(fixture).textContent ?? '').not.toMatch(
      /carrito|comprar|cotizar|agregar|añadir|IVA/i,
    );
  });
  // El badge de tres estados no distingue 1 de 10 ni 11 de 350, y quien repone por volumen
  // decide con esa diferencia. El numero exacto solo esta aqui, no en la tarjeta del listado.
  it('Render_ProductInStock_ShowsTheExactQuantityInTheSpecTable', async () => {
    const fixture = await render({ kind: 'ok', value: { ...product, stock: 120 } });

    expect(dom(fixture).querySelector('[data-testid="spec-stock"]')?.textContent?.trim()).toBe(
      '120 unidades',
    );
  });

  it('Render_SingleUnitLeft_UsesTheSingular', async () => {
    const fixture = await render({ kind: 'ok', value: { ...product, stock: 1 } });

    expect(dom(fixture).querySelector('[data-testid="spec-stock"]')?.textContent?.trim()).toBe(
      '1 unidad',
    );
  });

  it('Render_OutOfStock_SaysThereAreNoUnits', async () => {
    const fixture = await render({ kind: 'ok', value: { ...product, stock: 0 } });

    expect(dom(fixture).querySelector('[data-testid="spec-stock"]')?.textContent?.trim()).toBe(
      'Sin unidades',
    );
  });
});
