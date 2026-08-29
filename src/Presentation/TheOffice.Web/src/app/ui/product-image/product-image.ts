import { ChangeDetectionStrategy, Component, computed, input, linkedSignal } from '@angular/core';

/**
 * Imagen de producto con proporcion fija 3:2. La proporcion la impone el contenedor y no la
 * imagen, para que una foto que falta o que falla no mueva el resto de la cuadricula.
 *
 * "Sin imagen" es un estado normal del catalogo, no un error: se pinta en gris, nunca en rojo.
 */
@Component({
  selector: 'app-product-image',
  templateUrl: './product-image.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductImage {
  readonly src = input.required<string>();
  readonly alt = input.required<string>();
  readonly size = input<'card' | 'detail'>('card');
  /**
   * `loading="lazy"` en la imagen que resulta ser el LCP retrasa la primera pintura y Angular lo
   * avisa con NG0913. Las de la primera fila y la del detalle se piden en `eager`.
   */
  readonly priority = input(false);

  // Se reinicia cuando cambia la URL: una imagen rota no debe condenar a la siguiente.
  private readonly failed = linkedSignal<string, boolean>({
    source: this.src,
    computation: () => false,
  });

  protected readonly hasImage = computed(() => this.src().trim().length > 0 && !this.failed());

  protected readonly radius = computed(() =>
    this.size() === 'detail' ? 'rounded-lg' : 'rounded-md',
  );

  protected readonly loading = computed(() =>
    this.priority() || this.size() === 'detail' ? 'eager' : 'lazy',
  );

  protected onError(): void {
    this.failed.set(true);
  }
}
