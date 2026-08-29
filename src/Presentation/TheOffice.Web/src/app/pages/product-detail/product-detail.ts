import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { Fetched, ProductDetail } from '../../catalog/catalog.models';
import { CatalogService } from '../../catalog/catalog.service';
import { PricePipe } from '../../shared/price.pipe';
import { Button } from '../../ui/button/button';
import { CategoryChip } from '../../ui/category-chip/category-chip';
import { DiscontinuedBadge } from '../../ui/discontinued-badge/discontinued-badge';
import { EmptyState } from '../../ui/empty-state/empty-state';
import { ErrorState } from '../../ui/error-state/error-state';
import { ProductImage } from '../../ui/product-image/product-image';
import { StockBadge } from '../../ui/stock-badge/stock-badge';

/** `Fetched` mas el estado que no viene del servidor: la espera. */
type DetailState = { readonly kind: 'loading' } | Fetched<ProductDetail>;

const LOADING: DetailState = { kind: 'loading' };

/** Cuanto dura visible el "Copiado". Suficiente para leerlo, corto para no quedarse pegado. */
const COPIED_MS = 2500;

/**
 * Ficha de detalle de un producto. Informa; no vende: no hay ningun CTA de compra, ni carrito,
 * ni cotizacion. El comprador se lleva de aqui el SKU y el precio unitario.
 *
 * El SKU llega como `input` porque el router usa `withComponentInputBinding()`; los filtros del
 * listado viajan como query params y se devuelven intactos en el enlace de regreso, que es lo
 * unico que hace que "volver" no pierda la pagina ni la categoria en la que estaba el comprador.
 */
@Component({
  selector: 'app-product-detail',
  imports: [
    RouterLink,
    PricePipe,
    Button,
    CategoryChip,
    DiscontinuedBadge,
    EmptyState,
    ErrorState,
    ProductImage,
    StockBadge,
  ],
  templateUrl: './product-detail.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductDetailPage {
  readonly publicId = input.required<string>();

  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Cambia con cada "Reintentar": es lo que vuelve a disparar la peticion. */
  private readonly attempt = signal(0);

  private readonly state = signal<DetailState>(LOADING);

  private readonly params = toSignal(this.route.queryParamMap);

  protected readonly copied = signal(false);
  private copiedTimer: ReturnType<typeof setTimeout> | null = null;

  protected readonly loading = computed(() => this.state().kind === 'loading');
  protected readonly failed = computed(() => this.state().kind === 'error');
  protected readonly missing = computed(() => this.state().kind === 'not-found');

  protected readonly product = computed<ProductDetail | null>(() => {
    const state = this.state();

    return state.kind === 'ok' ? state.value : null;
  });

  /**
   * Los query params con los que se llego, tal cual, para devolverselos al listado. No se
   * interpretan aqui: el listado es el dueno de su propio contrato de filtros.
   */
  protected readonly backParams = computed<Record<string, string>>(() => {
    const map = this.params();
    if (!map) {
      return {};
    }

    const params: Record<string, string> = {};
    for (const key of map.keys) {
      const value = map.get(key);
      if (value !== null) {
        params[key] = value;
      }
    }

    return params;
  });

  constructor() {
    effect((onCleanup) => {
      const publicId = this.publicId();
      this.attempt();

      this.state.set(LOADING);
      const subscription = this.catalog
        .getProduct(publicId)
        .subscribe((result) => this.state.set(result));

      // Una peticion vieja no puede pisar a la nueva si el SKU cambia a mitad de camino.
      onCleanup(() => subscription.unsubscribe());
    });

    this.destroyRef.onDestroy(() => this.clearCopiedTimer());
  }

  protected onRetry(): void {
    this.attempt.update((attempt) => attempt + 1);
  }

  protected onCopy(): void {
    void this.copySku();
  }

  protected goToCatalog(): void {
    void this.router.navigate(['/productos']);
  }

  /**
   * El portapapeles puede no existir (contexto no seguro) o negarse por permisos. Si falla no se
   * confirma nada y ya: el SKU sigue en pantalla, seleccionable a mano. No es un error que
   * merezca interrumpir a nadie.
   */
  private async copySku(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.publicId());
    } catch {
      return;
    }

    this.clearCopiedTimer();
    this.copied.set(true);
    this.copiedTimer = setTimeout(() => {
      this.copied.set(false);
      this.copiedTimer = null;
    }, COPIED_MS);
  }

  private clearCopiedTimer(): void {
    if (this.copiedTimer !== null) {
      clearTimeout(this.copiedTimer);
      this.copiedTimer = null;
    }
  }
}
