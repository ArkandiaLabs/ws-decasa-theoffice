import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductImage } from './product-image';

describe('ProductImage', () => {
  let fixture: ComponentFixture<ProductImage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ProductImage] }).compileComponents();
    fixture = TestBed.createComponent(ProductImage);
  });

  async function render(src: string, alt = 'Silla ergonomica Aura'): Promise<void> {
    fixture.componentRef.setInput('src', src);
    fixture.componentRef.setInput('alt', alt);
    await fixture.whenStable();
  }

  function container(): HTMLElement {
    return fixture.nativeElement.querySelector('div') as HTMLElement;
  }

  function image(): HTMLImageElement | null {
    return fixture.nativeElement.querySelector('img');
  }

  // El marcador es el unico div anidado dentro del contenedor de proporcion.
  function placeholder(): HTMLElement | null {
    return container().querySelector('div');
  }

  it('Render_EmptySrc_ShowsThePlaceholderAndNoImage', async () => {
    await render('');

    expect(image()).toBeNull();
    expect(placeholder()?.textContent?.trim()).toBe('Sin imagen');
    expect(placeholder()?.classList).toContain('text-text-muted');
    expect(placeholder()?.classList).toContain('text-caption');
  });

  it('Render_BlankSrc_ShowsThePlaceholder', async () => {
    await render('   ');

    expect(image()).toBeNull();
    expect(placeholder()).not.toBeNull();
  });

  it('Render_ValidSrc_ShowsTheImageWithTheGivenAlt', async () => {
    await render('/assets/silla.webp', 'Silla ergonomica Aura');

    const img = image();
    expect(img).not.toBeNull();
    expect(img?.getAttribute('alt')).toBe('Silla ergonomica Aura');
    expect(img?.getAttribute('src')).toBe('/assets/silla.webp');
    expect(img?.getAttribute('loading')).toBe('lazy');
    expect(img?.classList).toContain('object-cover');
    expect(placeholder()).toBeNull();
  });

  it('Render_ValidSrc_KeepsTheThreeByTwoRatio', async () => {
    await render('/assets/silla.webp');

    expect(container().classList).toContain('aspect-3/2');
    expect(container().classList).toContain('overflow-hidden');
    expect(container().classList).toContain('bg-skeleton');
  });

  it('Render_EmptySrc_KeepsTheThreeByTwoRatio', async () => {
    await render('');

    expect(container().classList).toContain('aspect-3/2');
    expect(container().classList).toContain('bg-skeleton');
  });

  it('ImageError_BrokenSrc_FallsBackToThePlaceholder', async () => {
    await render('/assets/rota.webp');
    expect(image()).not.toBeNull();

    image()?.dispatchEvent(new Event('error'));
    await fixture.whenStable();

    expect(image()).toBeNull();
    expect(placeholder()).not.toBeNull();
    expect(container().classList).toContain('aspect-3/2');
  });

  it('ImageError_SrcChangesAfterFailure_TriesTheNewImageAgain', async () => {
    await render('/assets/rota.webp');
    image()?.dispatchEvent(new Event('error'));
    await fixture.whenStable();
    expect(image()).toBeNull();

    await render('/assets/buena.webp');

    expect(image()).not.toBeNull();
    expect(image()?.getAttribute('src')).toBe('/assets/buena.webp');
  });

  it('Render_DefaultSize_UsesTheCardRadius', async () => {
    await render('/assets/silla.webp');

    expect(container().classList).toContain('rounded-md');
  });

  it('Render_DetailSize_UsesTheLargerRadius', async () => {
    await render('/assets/silla.webp');
    fixture.componentRef.setInput('size', 'detail');
    await fixture.whenStable();

    expect(container().classList).toContain('rounded-lg');
  });
});
