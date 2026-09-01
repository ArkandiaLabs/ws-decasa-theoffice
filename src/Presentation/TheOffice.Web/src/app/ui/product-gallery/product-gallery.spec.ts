import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductImage } from '../../catalog/catalog.models';
import { ProductGallery } from './product-gallery';

function photo(number: number, overrides: Partial<ProductImage> = {}): ProductImage {
  return {
    publicId: `PRD-013-IMG-${number}`,
    url: `/img/prd-013-${number}.jpg`,
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

  async function render(
    images: readonly ProductImage[],
    productName = 'Organizador de escritorio',
  ): Promise<void> {
    fixture.componentRef.setInput('images', images);
    fixture.componentRef.setInput('productName', productName);
    await fixture.whenStable();
  }

  function dom(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function main(): HTMLImageElement | null {
    return dom().querySelector('img[loading="eager"]');
  }

  function tabs(): HTMLButtonElement[] {
    return [...dom().querySelectorAll<HTMLButtonElement>('[role="tab"]')];
  }

  function live(): string {
    return dom().querySelector('[role="status"]')?.textContent?.trim() ?? '';
  }

  it('Render_SinglePhoto_HidesTheThumbnailsAndTheArrows', async () => {
    await render([photo(1)]);

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-1.jpg');
    expect(dom().querySelector('[role="tablist"]')).toBeNull();
    expect(dom().querySelector('[aria-label="Foto siguiente"]')).toBeNull();
    expect(dom().querySelector('[aria-label="Foto anterior"]')).toBeNull();
  });

  it('Render_SinglePhoto_UsesTheProductNameAsAlt', async () => {
    await render([photo(1)], 'Bolígrafo tinta negra x12');

    expect(main()?.getAttribute('alt')).toBe('Bolígrafo tinta negra x12');
  });

  it('Render_SeveralPhotos_ShowsOneThumbnailPerPhotoAndTheArrows', async () => {
    await render([photo(1), photo(2), photo(3)]);

    expect(tabs().length).toBe(3);
    expect(dom().querySelector('[aria-label="Foto siguiente"]')).toBeTruthy();
    expect(dom().querySelector('[aria-label="Foto anterior"]')).toBeTruthy();
    expect(main()?.getAttribute('alt')).toBe('Organizador de escritorio, foto 1 de 3');
  });

  // El servidor ordena por sortOrder, no por la marca: la principal puede estar en cualquier sitio.
  it('Render_PrimaryIsNotTheFirst_OpensOnThePrimary', async () => {
    await render([
      photo(1, { isPrimary: false }),
      photo(2, { isPrimary: true }),
      photo(3, { isPrimary: false }),
    ]);

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-2.jpg');
    expect(tabs()[1].getAttribute('aria-selected')).toBe('true');
    expect(live()).toBe('Foto 2 de 3');
  });

  it('Render_NoPrimaryMarked_OpensOnTheFirst', async () => {
    await render([photo(1, { isPrimary: false }), photo(2, { isPrimary: false })]);

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-1.jpg');
    expect(tabs()[0].getAttribute('aria-selected')).toBe('true');
  });

  it('Select_ClickOnAThumbnail_SwapsTheMainPhoto', async () => {
    await render([photo(1), photo(2), photo(3)]);

    tabs()[2].click();
    await fixture.whenStable();

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-3.jpg');
    expect(tabs()[2].getAttribute('aria-selected')).toBe('true');
    expect(tabs()[0].getAttribute('aria-selected')).toBe('false');
    expect(live()).toBe('Foto 3 de 3');
  });

  it('Next_OnTheLastPhoto_WrapsAroundToTheFirst', async () => {
    await render([photo(1), photo(2)]);

    const next = dom().querySelector<HTMLButtonElement>('[aria-label="Foto siguiente"]');
    next?.click();
    await fixture.whenStable();
    expect(main()?.getAttribute('src')).toBe('/img/prd-013-2.jpg');

    next?.click();
    await fixture.whenStable();
    expect(main()?.getAttribute('src')).toBe('/img/prd-013-1.jpg');
  });

  it('Previous_OnTheFirstPhoto_WrapsAroundToTheLast', async () => {
    await render([photo(1), photo(2), photo(3)]);

    dom().querySelector<HTMLButtonElement>('[aria-label="Foto anterior"]')?.click();
    await fixture.whenStable();

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-3.jpg');
  });

  it('Render_SeveralPhotos_KeepsOnlyTheSelectedThumbnailInTheTabOrder', async () => {
    await render([photo(1), photo(2), photo(3)]);

    expect(tabs().map((tab) => tab.getAttribute('tabindex'))).toEqual(['0', '-1', '-1']);

    tabs()[1].click();
    await fixture.whenStable();

    expect(tabs().map((tab) => tab.getAttribute('tabindex'))).toEqual(['-1', '0', '-1']);
  });

  // El teclado se maneja en la pestana enfocada, que bajo `tabindex` movil es la seleccionada.
  async function press(key: string): Promise<void> {
    const focused = tabs().find((tab) => tab.getAttribute('aria-selected') === 'true') ?? tabs()[0];
    focused.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
    await fixture.whenStable();
  }

  it('KeyDown_ArrowRight_MovesTheSelectionAndTheFocus', async () => {
    await render([photo(1), photo(2), photo(3)]);
    tabs()[0].focus();

    await press('ArrowRight');

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-2.jpg');
    expect(document.activeElement).toBe(tabs()[1]);
  });

  it('KeyDown_ArrowLeftOnTheFirst_WrapsAroundToTheLast', async () => {
    await render([photo(1), photo(2), photo(3)]);

    await press('ArrowLeft');

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-3.jpg');
    expect(document.activeElement).toBe(tabs()[2]);
  });

  it('KeyDown_HomeAndEnd_JumpToTheEdges', async () => {
    await render([photo(1), photo(2), photo(3), photo(4)]);

    await press('End');
    expect(main()?.getAttribute('src')).toBe('/img/prd-013-4.jpg');

    await press('Home');
    expect(main()?.getAttribute('src')).toBe('/img/prd-013-1.jpg');
  });

  it('KeyDown_UnhandledKey_LeavesTheSelectionAlone', async () => {
    await render([photo(1), photo(2)]);

    await press('ArrowDown');

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-1.jpg');
  });

  it('Render_EmptyGallery_ShowsThePlaceholderAndNoControls', async () => {
    await render([]);

    expect(main()).toBeNull();
    expect(dom().textContent).toContain('Sin imagen');
    expect(dom().querySelector('[role="tablist"]')).toBeNull();
    expect(live()).toBe('');
  });

  it('Render_BlankUrl_ShowsThePlaceholder', async () => {
    await render([photo(1, { url: '   ' })]);

    expect(main()).toBeNull();
    expect(dom().textContent).toContain('Sin imagen');
  });

  it('ImageError_MainPhotoFails_FallsBackToThePlaceholderWithoutTouchingTheOthers', async () => {
    await render([photo(1), photo(2)]);

    main()?.dispatchEvent(new Event('error'));
    await fixture.whenStable();

    expect(main()).toBeNull();
    expect(dom().textContent).toContain('Sin imagen');

    dom().querySelector<HTMLButtonElement>('[aria-label="Foto siguiente"]')?.click();
    await fixture.whenStable();

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-2.jpg');
  });

  it('ImageError_ThumbnailFails_FallsBackToTheIcon', async () => {
    await render([photo(1), photo(2)]);

    const broken = tabs()[1].querySelector('img');
    expect(broken).not.toBeNull();

    broken?.dispatchEvent(new Event('error'));
    await fixture.whenStable();

    expect(tabs()[1].querySelector('img')).toBeNull();
    expect(tabs()[1].querySelector('svg')).toBeTruthy();
  });

  // Otro producto, otra galeria: la seleccion y las fotos rotas del anterior no se heredan.
  it('Render_NewGallery_ResetsTheSelectionAndTheFailures', async () => {
    await render([photo(1), photo(2), photo(3)]);
    tabs()[2].click();
    main()?.dispatchEvent(new Event('error'));
    await fixture.whenStable();

    await render([photo(1), photo(2)]);

    expect(main()?.getAttribute('src')).toBe('/img/prd-013-1.jpg');
    expect(live()).toBe('Foto 1 de 2');
  });

  // Vitest corre sin navegador y no mide nada, pero el contrato de clases si se puede fijar: la
  // miniatura ya se envio una vez clavada en 44x44, el minimo tactil, y a ese tamano la foto es un
  // borron. `aspect-3/2` la mantiene con el mismo encuadre que la foto grande.
  it('Render_SeveralPhotos_SizesTheThumbnailsAboveTheTapMinimumAndCropsThemThreeByTwo', async () => {
    await render([photo(1), photo(2)]);

    for (const tab of tabs()) {
      expect(tab.classList).toContain('aspect-3/2');
      expect(tab.classList).toContain('flex-1');
      expect(tab.classList).toContain('max-w-30');
      expect(tab.classList).toContain('min-w-11');
      expect(tab.classList).toContain('min-h-11');
      expect(tab.classList).not.toContain('h-11');
      expect(tab.classList).not.toContain('w-11');
    }
  });

  it('Render_Always_NamesTheGalleryAndItsThumbnails', async () => {
    await render([photo(1), photo(2)], 'Monitor 27 pulgadas QHD');

    expect(dom().querySelector('[role="group"]')?.getAttribute('aria-label')).toBe(
      'Galería de fotos de Monitor 27 pulgadas QHD',
    );
    expect(dom().querySelector('[role="tablist"]')?.getAttribute('aria-label')).toBe(
      'Miniaturas de fotos',
    );
    expect(tabs().map((tab) => tab.getAttribute('aria-label'))).toEqual([
      'Foto 1 de 2',
      'Foto 2 de 2',
    ]);
  });
});
