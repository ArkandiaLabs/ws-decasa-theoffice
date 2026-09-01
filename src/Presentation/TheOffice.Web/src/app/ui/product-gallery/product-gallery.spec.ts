import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductPhoto } from '../../catalog/catalog.models';
import { ProductGallery } from './product-gallery';

const NAME = 'Resma de papel carta 75 g';

function photo(number: number, overrides: Partial<ProductPhoto> = {}): ProductPhoto {
  return {
    publicId: `PRD-001-IMG-${number}`,
    url: `/img/prd-001-${number}.jpg`,
    sortOrder: number - 1,
    isPrimary: number === 1,
    ...overrides,
  };
}

describe('ProductGallery', () => {
  let fixture: ComponentFixture<ProductGallery>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ProductGallery] }).compileComponents();
    fixture = TestBed.createComponent(ProductGallery);
  });

  async function render(photos: readonly ProductPhoto[]): Promise<void> {
    fixture.componentRef.setInput('photos', photos);
    fixture.componentRef.setInput('productName', NAME);
    await fixture.whenStable();
  }

  function dom(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function thumbs(): HTMLButtonElement[] {
    return Array.from(dom().querySelectorAll('button'));
  }

  function main(): HTMLImageElement | null {
    return dom().querySelector('app-product-image img');
  }

  function strip(): HTMLElement | null {
    return dom().querySelector('[role="group"]');
  }

  it('Render_SinglePhoto_ShowsNoNavigationControls', async () => {
    await render([photo(1)]);

    expect(thumbs()).toHaveLength(0);
    expect(strip()).toBeNull();
  });

  // El borde en el que aparece la tira: con dos fotos ya hay que poder elegir.
  it('Render_TwoPhotos_ShowsTheThumbnailStrip', async () => {
    await render([photo(1), photo(2)]);

    expect(thumbs()).toHaveLength(2);
    expect(strip()).not.toBeNull();
  });

  // La API rechaza una galeria vacia hoy, pero el tipo la permite y la ficha no puede reventar.
  it('Render_NoPhotos_ShowsThePlaceholder', async () => {
    await render([]);

    expect(thumbs()).toHaveLength(0);
    expect(main()).toBeNull();
    expect(dom().textContent).toContain('Sin imagen');
  });

  // El tipo dice que `images` siempre viene; la respuesta HTTP no lo garantiza y el molde de
  // TypeScript no valida nada en tiempo de ejecucion.
  it('Render_PhotosMissingFromThePayload_DoesNotThrow', async () => {
    await render(undefined as unknown as readonly ProductPhoto[]);

    expect(thumbs()).toHaveLength(0);
    expect(dom().textContent).toContain('Sin imagen');
  });

  // El servidor promete ordenar por sortOrder; la ficha no depende de que lo cumpla.
  it('Render_UnsortedPhotos_OrdersThumbnailsBySortOrder', async () => {
    await render([photo(3), photo(1), photo(2)]);

    const urls = thumbs().map((button) => button.querySelector('img')?.getAttribute('src'));

    expect(urls).toEqual(['/img/prd-001-1.jpg', '/img/prd-001-2.jpg', '/img/prd-001-3.jpg']);
  });

  // El contrato v2 ordena por sortOrder, no por isPrimary: la principal puede no ser la primera.
  it('Render_PrimaryNotFirst_StartsOnThePrimary', async () => {
    await render([
      photo(1, { isPrimary: false }),
      photo(2, { isPrimary: false }),
      photo(3, { isPrimary: true }),
    ]);

    expect(main()?.getAttribute('src')).toBe('/img/prd-001-3.jpg');
  });

  it('Render_NoPhotoIsPrimary_StartsOnTheFirst', async () => {
    await render([photo(1, { isPrimary: false }), photo(2, { isPrimary: false })]);

    expect(main()?.getAttribute('src')).toBe('/img/prd-001-1.jpg');
  });

  it('Click_Thumbnail_SwapsTheMainImage', async () => {
    await render([photo(1), photo(2), photo(3)]);

    thumbs()[2].click();
    await fixture.whenStable();

    expect(main()?.getAttribute('src')).toBe('/img/prd-001-3.jpg');
  });

  it('Click_Thumbnail_MarksItAsCurrent', async () => {
    await render([photo(1), photo(2), photo(3)]);

    thumbs()[1].click();
    await fixture.whenStable();

    expect(thumbs()[1].getAttribute('aria-current')).toBe('true');
    expect(thumbs()[0].getAttribute('aria-current')).toBeNull();
  });

  // jsdom nunca carga imagenes, asi que el fallo se dispara a mano.
  it('ImageError_Thumbnail_ShowsThePlaceholderForThatPhotoOnly', async () => {
    await render([photo(1), photo(2), photo(3)]);

    thumbs()[1].querySelector('img')?.dispatchEvent(new Event('error'));
    await fixture.whenStable();

    expect(thumbs()[1].textContent?.trim()).toBe('Sin imagen');
    expect(thumbs()[1].querySelector('img')).toBeNull();
    expect(thumbs()[0].querySelector('img')).not.toBeNull();
    expect(thumbs()[2].querySelector('img')).not.toBeNull();
  });

  // El router reusa la instancia entre fichas: un indice viejo no puede sobrevivir al cambio.
  it('Render_PhotosChange_ResetsToThePrimary', async () => {
    await render([photo(1), photo(2), photo(3), photo(4)]);
    thumbs()[3].click();
    await fixture.whenStable();

    await render([photo(1)]);

    expect(main()?.getAttribute('src')).toBe('/img/prd-001-1.jpg');
    expect(thumbs()).toHaveLength(0);
  });
});
