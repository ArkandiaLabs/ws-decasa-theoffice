import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Params, Router } from '@angular/router';
import { map } from 'rxjs';

import { Category, PagedResult, ProductListItem, ProductQuery } from '../../catalog/catalog.models';
import { CatalogService, PAGE_SIZE } from '../../catalog/catalog.service';
import { CategoryChip } from '../../ui/category-chip/category-chip';
import { EmptyState } from '../../ui/empty-state/empty-state';
import { ErrorState } from '../../ui/error-state/error-state';
import { Pagination } from '../../ui/pagination/pagination';
import { ProductCard } from '../../ui/product-card/product-card';
import { SearchField } from '../../ui/search-field/search-field';
import { SkeletonCard } from '../../ui/skeleton-card/skeleton-card';

/**
 * SKU completo y exacto: `PRD-` mas tres digitos. Un termino parcial (`PRD-0`) no califica y
 * cae en la busqueda normal, que es lo que espera quien todavia esta escribiendo.
 */
const SKU_PATTERN = /^PRD-\d{3}$/;

/** Los tres desenlaces de la pantalla, como un solo valor: no hay banderas que puedan mentir. */
type ListState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'ready'; readonly result: PagedResult<ProductListItem> }
  | { readonly kind: 'error' };

/**
 * Listado del catalogo. Los filtros viven en la URL, no en el componente: la peticion se dispara
 * desde los query params, asi que un enlace compartido y el boton "atras" del navegador
 * reconstruyen la misma pantalla sin codigo extra.
 */
@Component({
  selector: 'app-product-list',
  imports: [
    CategoryChip,
    EmptyState,
    ErrorState,
    Pagination,
    ProductCard,
    SearchField,
    SkeletonCard,
  ],
  templateUrl: './product-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductList {
  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  /** Tantas siluetas como productos trae una pagina: la grilla no salta cuando llegan los datos. */
  protected readonly skeletons = Array.from({ length: PAGE_SIZE }, (_, index) => index);

  private readonly params = toSignal(this.route.queryParamMap, {
    initialValue: convertToParamMap({}),
  });

  /** Una categoria que falla no rompe el listado: sin categorias, la fila de chips no se dibuja. */
  protected readonly categories = toSignal(
    this.catalog
      .getCategories()
      .pipe(
        map((fetched) => (fetched.kind === 'ok' ? fetched.value : ([] as readonly Category[]))),
      ),
    { initialValue: [] as readonly Category[] },
  );

  protected readonly page = computed(() => {
    const raw = Number(this.params().get('page'));

    return Number.isInteger(raw) && raw >= 1 ? raw : 1;
  });

  protected readonly category = computed(() => this.params().get('category') ?? '');
  protected readonly term = computed(() => this.params().get('search') ?? '');

  /** Se muestra el nombre tal como llega del servidor; el slug es el respaldo si no hay categorias. */
  protected readonly categoryLabel = computed(() => {
    const slug = this.category();
    if (!slug) {
      return '';
    }

    return this.categories().find((item) => item.slug === slug)?.name ?? slug;
  });

  private readonly attempt = signal(0);
  private readonly state = signal<ListState>({ kind: 'loading' });

  protected readonly loading = computed(() => this.state().kind === 'loading');
  protected readonly failed = computed(() => this.state().kind === 'error');
  protected readonly result = computed(() => {
    const state = this.state();

    return state.kind === 'ready' ? state.result : null;
  });

  protected readonly emptyMessage = computed(() => {
    const term = this.term();
    const category = this.categoryLabel();

    if (term && category) {
      return `No encontramos productos para «${term}» en ${category}.`;
    }
    if (term) {
      return `No encontramos productos para «${term}».`;
    }
    if (category) {
      return `No encontramos productos en ${category}.`;
    }

    return 'No encontramos productos con los filtros actuales.';
  });

  /** Un solo lugar habla por el lector de pantalla: carga, resultado o fallo, nunca los tres. */
  protected readonly announcement = computed(() => {
    const state = this.state();

    if (state.kind === 'loading') {
      return 'Cargando productos…';
    }
    if (state.kind === 'error') {
      return 'No pudimos cargar el catálogo.';
    }
    if (state.result.totalItems === 0) {
      return 'Ningún resultado.';
    }

    return state.result.totalItems === 1
      ? '1 resultado.'
      : `${state.result.totalItems} resultados.`;
  });

  constructor() {
    // Las dependencias del efecto son los tres valores primitivos de la URL, no el objeto que se
    // arma con ellos: una navegacion que no cambia ningun filtro no vuelve a pedir el listado.
    effect((onCleanup) => {
      const query: ProductQuery = {
        page: this.page(),
        pageSize: PAGE_SIZE,
        category: this.category() || undefined,
        search: this.term() || undefined,
      };
      // Leer el intento es lo que hace que "Reintentar" vuelva a pedir con los mismos filtros.
      this.attempt();

      this.state.set({ kind: 'loading' });

      const subscription = this.catalog.getProducts(query).subscribe((fetched) => {
        this.state.set(
          fetched.kind === 'ok' ? { kind: 'ready', result: fetched.value } : { kind: 'error' },
        );
      });

      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected onTerm(value: string): void {
    const term = value.trim();

    // El SKU exacto no es una busqueda: es una direccion. Se va al detalle sin pedir el listado.
    if (SKU_PATTERN.test(term)) {
      void this.router.navigate(['/productos', term]);
      return;
    }

    this.merge({ search: term || null, page: null });
  }

  protected onCategory(slug: string): void {
    this.merge({ category: slug || null, page: null });
  }

  protected onAllCategories(): void {
    this.merge({ category: null, page: null });
  }

  protected onClearFilters(): void {
    this.merge({ search: null, category: null, page: null });
  }

  protected onPage(page: number): void {
    this.merge({ page: page > 1 ? page : null });
  }

  protected onRetry(): void {
    this.attempt.update((value) => value + 1);
  }

  /**
   * Un valor nulo borra el parametro de la URL. Por eso cambiar de filtro manda `page: null` en
   * vez de `page: 1`: la primera pagina es la ausencia de parametro, no un parametro con un 1.
   */
  private merge(queryParams: Params): void {
    void this.router.navigate([], { queryParams, queryParamsHandling: 'merge' });
  }
}
