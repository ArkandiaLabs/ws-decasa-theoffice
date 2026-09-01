import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  input,
  linkedSignal,
  viewChildren,
} from '@angular/core';

import { ProductImage } from '../../catalog/catalog.models';

/** Lo que la plantilla necesita para pintar una miniatura: nada de la entidad, solo lo dibujable. */
interface Thumb {
  readonly index: number;
  readonly url: string;
  readonly label: string;
  readonly selected: boolean;
  readonly classes: string;
}

/**
 * La miniatura recorta en 3:2, igual que la foto grande: lo que se ve en la tira es exactamente
 * el encuadre que se va a abrir.
 *
 * El tamano es flexible entre 44 px (el minimo tactil, que es el suelo y nunca el objetivo) y
 * 120 px: la tira reparte el ancho disponible, asi que las fotos entran en una sola fila tanto en
 * los 600 px del escritorio como en los 360 px del telefono, y se ven tan grandes como quepan.
 * Un cuadro de 44 px es un borron: una foto de producto necesita area para distinguirse de la de
 * al lado, que es lo unico que la tira tiene que resolver.
 */
const THUMB =
  'flex aspect-3/2 min-h-11 min-w-11 max-w-30 flex-1 items-center justify-center overflow-hidden rounded-sm text-text-muted';
const THUMB_SELECTED = 'border-2 border-secondary bg-surface-raised';
const THUMB_IDLE = 'border border-secondary bg-surface';

/**
 * Galeria de fotos de un producto. La foto grande manda; las miniaturas son el unico control de
 * seleccion y las flechas son un atajo encima de la imagen.
 *
 * Con una sola foto no hay nada que navegar: no se pintan ni flechas ni miniaturas. Es el caso
 * comun del catalogo (un consumible tiene una toma) y llenarlo de controles inertes seria ruido.
 *
 * "Sin imagen" es un estado normal, no un error: se pinta en gris y con el mismo icono que la
 * miniatura de una foto rota, nunca en rojo.
 */
@Component({
  selector: 'app-product-gallery',
  templateUrl: './product-gallery.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductGallery {
  readonly images = input.required<readonly ProductImage[]>();
  readonly productName = input.required<string>();

  private readonly tabs = viewChildren<ElementRef<HTMLButtonElement>>('thumb');

  /**
   * Abre en la principal, no en la primera: el servidor ordena por `sortOrder`, asi que la foto
   * que el comprador acaba de ver en el listado puede estar en cualquier posicion.
   */
  private readonly selected = linkedSignal<readonly ProductImage[], number>({
    source: this.images,
    computation: (images) => {
      const primary = images.findIndex((image) => image.isPrimary);

      return primary < 0 ? 0 : primary;
    },
  });

  // Se reinicia con la galeria: una foto rota no debe condenar a la del siguiente producto.
  private readonly failed = linkedSignal<readonly ProductImage[], ReadonlySet<number>>({
    source: this.images,
    computation: () => new Set<number>(),
  });

  protected readonly count = computed(() => this.images().length);

  protected readonly hasMultiple = computed(() => this.count() > 1);

  protected readonly currentUrl = computed(() => this.url(this.selected()));

  protected readonly alt = computed(() =>
    this.hasMultiple()
      ? `${this.productName()}, foto ${this.selected() + 1} de ${this.count()}`
      : this.productName(),
  );

  /** El cambio de foto no mueve el foco por si solo: esta region es la que lo cuenta. */
  protected readonly liveText = computed(() =>
    this.count() === 0 ? '' : `Foto ${this.selected() + 1} de ${this.count()}`,
  );

  protected readonly thumbs = computed<readonly Thumb[]>(() =>
    this.images().map((image, index) => {
      const selected = index === this.selected();

      return {
        index,
        url: this.url(index),
        label: `Foto ${index + 1} de ${this.count()}`,
        selected,
        classes: `${THUMB} ${selected ? THUMB_SELECTED : THUMB_IDLE}`,
      };
    }),
  );

  protected select(index: number): void {
    this.selected.set(index);
  }

  protected previous(): void {
    this.step(-1);
  }

  protected next(): void {
    this.step(1);
  }

  /**
   * Teclado de la lista de pestanas, tal como lo pide APG: las flechas recorren, `Home` y `End`
   * saltan a los extremos, y el recorrido da la vuelta. El foco viaja con la seleccion porque el
   * `tabindex` es movil: la pestana que no esta seleccionada sale del orden de tabulacion.
   *
   * Va en cada pestana y no en la lista, que no es enfocable: un manejador de teclado colgado de
   * un contenedor que nadie puede enfocar es justo lo que la regla de accesibilidad del lint
   * senala, y aqui la unica pestana en el orden de tabulacion es la seleccionada.
   */
  protected onKeyDown(event: KeyboardEvent): void {
    const count = this.count();
    if (count < 2) {
      return;
    }

    const current = this.selected();
    let next: number;

    switch (event.key) {
      case 'ArrowRight':
        next = (current + 1) % count;
        break;
      case 'ArrowLeft':
        next = (current - 1 + count) % count;
        break;
      case 'Home':
        next = 0;
        break;
      case 'End':
        next = count - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    this.select(next);
    this.tabs()[next]?.nativeElement.focus();
  }

  protected onCurrentError(): void {
    this.onError(this.selected());
  }

  protected onError(index: number): void {
    this.failed.update((failed) => new Set(failed).add(index));
  }

  private step(delta: number): void {
    const count = this.count();
    if (count === 0) {
      return;
    }

    this.select((this.selected() + delta + count) % count);
  }

  /** Cadena vacia significa "no hay foto que pintar": ni URL, ni una que ya fallo al cargar. */
  private url(index: number): string {
    if (this.failed().has(index)) {
      return '';
    }

    return this.images()[index]?.url.trim() ?? '';
  }
}
