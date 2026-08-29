import { registerLocaleData } from '@angular/common';
import localeEsCo from '@angular/common/locales/es-CO';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { ProductListItem } from '../../catalog/catalog.models';
import { ProductCard } from './product-card';

// El precio se formatea con los datos del locale es-CO. Sin registrarlo, `formatNumber` cae a
// en-US y la prueba pasaria contra un formato que el usuario nunca ve.
registerLocaleData(localeEsCo);

const baseProduct: ProductListItem = {
  publicId: 'PRD-001',
  name: 'Resma de papel carta 75 g',
  price: 18900,
  imageUrl: '/img/prd-001.jpg',
  stock: 42,
  categoryName: 'Papelería',
  categorySlug: 'papeleria',
};

describe('ProductCard', () => {
  async function render(product: ProductListItem): Promise<ComponentFixture<ProductCard>> {
    const fixture = TestBed.createComponent(ProductCard);
    fixture.componentRef.setInput('product', product);
    await fixture.whenStable();

    return fixture;
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductCard],
      providers: [
        // Rutas reales minimas: el `href` que produce RouterLink depende de que la ruta exista.
        provideRouter([
          { path: 'productos', children: [] },
          { path: 'productos/:publicId', children: [] },
        ]),
        { provide: LOCALE_ID, useValue: 'es-CO' },
      ],
    }).compileComponents();
  });

  it('Render_ProductWithCategory_ShowsTheCategoryTagOverTheImage', async () => {
    const fixture = await render(baseProduct);

    const tag = fixture.nativeElement.querySelector('[data-testid="category-tag"]');
    expect(tag).toBeTruthy();
    expect(tag.textContent.trim()).toBe('Papelería');
  });

  it('Render_ProductWithoutCategory_OmitsTheCategoryTag', async () => {
    const fixture = await render({ ...baseProduct, categoryName: '' });

    expect(fixture.nativeElement.querySelector('[data-testid="category-tag"]')).toBeNull();
  });

  it('Render_Always_ShowsThePublicIdAsSku', async () => {
    const fixture = await render(baseProduct);

    expect(fixture.nativeElement.textContent).toContain('PRD-001');
  });

  it('Render_Price_FormatsItInColombianPesos', async () => {
    const fixture = await render(baseProduct);

    expect(fixture.nativeElement.textContent).toContain('$ 18.900');
  });

  it('Render_LongNameAndSevenDigitPrice_RendersBoth', async () => {
    const fixture = await render({
      ...baseProduct,
      name: 'Organizador de escritorio 5 compartimentos',
      price: 1250000,
    });

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Organizador de escritorio 5 compartimentos');
    expect(text).toContain('$ 1.250.000');
  });

  it('Render_Always_LinksToTheProductDetail', async () => {
    const fixture = await render(baseProduct);

    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('a');
    expect(link.getAttribute('href')).toBe('/productos/PRD-001');
    expect(link.textContent.trim()).toBe('Ver detalle →');
  });
  // El enlace del detalle arrastra los filtros del listado. Sin esto, entrar a una ficha desde
  // la pagina 2 y volver deja al comprador en la pagina 1, buscando de nuevo donde estaba.
  it('Render_ListWithActiveFilters_KeepsThemInTheDetailLink', async () => {
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/productos?page=2&category=papeleria');
    const fixture = await render(baseProduct);
    const link = fixture.nativeElement.querySelector('a[href*="/productos/PRD-001"]');

    expect(link.getAttribute('href')).toContain('page=2');
    expect(link.getAttribute('href')).toContain('category=papeleria');
  });

  it('Render_PriorityCard_AsksForTheImageEagerly', async () => {
    const fixture = TestBed.createComponent(ProductCard);
    fixture.componentRef.setInput('product', baseProduct);
    fixture.componentRef.setInput('priority', true);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('img').getAttribute('loading')).toBe('eager');
  });

  it('Render_NonPriorityCard_DefersTheImage', async () => {
    const fixture = await render(baseProduct);

    expect(fixture.nativeElement.querySelector('img').getAttribute('loading')).toBe('lazy');
  });
});
