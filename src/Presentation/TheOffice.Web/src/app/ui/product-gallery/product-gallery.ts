import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  signal,
} from '@angular/core';

import { ProductPhoto } from '../../catalog/catalog.models';
import { ProductImage } from '../product-image/product-image';

// Literales completos en constantes: una clase interpolada no la genera Tailwind ni la reporta
// check-classes.mjs, asi que se pierde en silencio.
const THUMB = 'block h-16 w-24 overflow-hidden rounded-sm bg-neutral sm:h-24 sm:w-36';
const ACTIVE = 'border-2 border-foreground p-1';
const INACTIVE = 'border border-secondary hover:border-border-strong';

/**
 * Galeria de la ficha: una imagen grande mas una tira de miniaturas. Con una sola foto no se
 * renderiza ningun control de navegacion.
 *
 * La imagen grande es inerte: solo cambia al elegir una miniatura. Sin lightbox, sin zoom, sin
 * swipe y sin libreria de carrusel.
 */
@Component({
  selector: 'app-product-gallery',
  imports: [ProductImage],
  templateUrl: './product-gallery.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductGallery {
  readonly photos = input.required<readonly ProductPhoto[]>();
  readonly productName = input.required<string>();

  // El servidor promete ordenar por SortOrder, pero el contrato ya se describio mal una vez: se
  // reordena aqui con su misma regla en vez de confiar.
  protected readonly ordered = computed(() =>
    [...this.photos()].sort(
      (left, right) =>
        left.sortOrder - right.sortOrder || left.publicId.localeCompare(right.publicId),
    ),
  );

  // Arranca en la marcada como principal, o en la primera si ninguna lo esta. Se reinicia cuando
  // cambia el producto: el router reusa la instancia entre fichas y un indice viejo quedaria fuera.
  protected readonly selected = linkedSignal<readonly ProductPhoto[], number>({
    source: this.ordered,
    computation: (photos) =>
      Math.max(
        photos.findIndex((photo) => photo.isPrimary),
        0,
      ),
  });

  // El fallo tiene que sobrevivir al cambio de seleccion, asi que no puede ser un linkedSignal.
  private readonly broken = signal<ReadonlySet<string>>(new Set());

  protected readonly hasStrip = computed(() => this.ordered().length > 1);

  protected readonly total = computed(() => this.ordered().length);

  // Una galeria vacia no es alcanzable por la API, pero el tipo la permite: la cadena vacia hace
  // que ProductImage caiga a su marcador en vez de reventar la ficha entera.
  protected readonly activeUrl = computed(() => this.ordered()[this.selected()]?.url ?? '');

  protected isActive(index: number): boolean {
    return index === this.selected();
  }

  protected thumbClass(index: number): string {
    return `${THUMB} ${this.isActive(index) ? ACTIVE : INACTIVE}`;
  }

  protected isBroken(publicId: string): boolean {
    return this.broken().has(publicId);
  }

  protected onSelect(index: number): void {
    this.selected.set(index);
  }

  protected onThumbError(publicId: string): void {
    this.broken.update((failed) => new Set(failed).add(publicId));
  }
}
