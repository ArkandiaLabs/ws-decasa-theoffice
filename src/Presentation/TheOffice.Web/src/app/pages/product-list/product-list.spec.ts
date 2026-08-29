import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, Subject } from 'rxjs';

import {
  Category,
  Fetched,
  PagedResult,
  ProductListItem,
  ProductQuery,
} from '../../catalog/catalog.models';
import { CatalogService, PAGE_SIZE } from '../../catalog/catalog.service';
import { ProductList } from './product-list';

/** Mismo valor que usa `SearchField`; aqui se avanza el reloj a mano. */
const DEBOUNCE_MS = 300;

type ProductsResponse = Fetched<PagedResult<ProductListItem>>;
type CategoriesResponse = Fetched<readonly Category[]>;

@Component({ selector: 'app-route-stub', template: '' })
class RouteStub {}

const categories: readonly Category[] = [
  { publicId: 'CAT-001', name: 'Papeleria', slug: 'papeleria', description: '' },
  { publicId: 'CAT-002', name: 'Tecnologia', slug: 'tecnologia', description: '' },
  { publicId: 'CAT-003', name: 'Mobiliario', slug: 'mobiliario', description: '' },
  { publicId: 'CAT-004', name: 'Organizacion', slug: 'organizacion', description: '' },
];

function product(publicId: string, name: string): ProductListItem {
  return {
    publicId,
    name,
    price: 18900,
    imageUrl: '',
    stock: 120,
    categoryName: 'Papeleria',
    categorySlug: 'papeleria',
  };
}

function paged(
  items: readonly ProductListItem[],
  overrides: Partial<PagedResult<ProductListItem>> = {},
): PagedResult<ProductListItem> {
  return {
    items,
    page: 1,
    pageSize: PAGE_SIZE,
    totalItems: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    ...overrides,
  };
}

const fullPage: ProductsResponse = {
  kind: 'ok',
  value: paged([product('PRD-001', 'Resma de papel carta 75g')], { totalItems: 16, totalPages: 2 }),
};

interface RenderOptions {
  readonly url?: string;
  readonly products?: () => Observable<ProductsResponse>;
  readonly categories?: CategoriesResponse;
}

interface Rendered {
  readonly fixture: ComponentFixture<ProductList>;
  readonly host: HTMLElement;
  readonly router: Router;
  readonly catalog: {
    getProducts: ReturnType<typeof vi.fn>;
    getCategories: ReturnType<typeof vi.fn>;
    getProduct: ReturnType<typeof vi.fn>;
  };
}

async function render(options: RenderOptions = {}): Promise<Rendered> {
  const products = options.products ?? (() => of(fullPage));
  const catalog = {
    getProducts: vi.fn(() => products()),
    getCategories: vi.fn(() =>
      of(options.categories ?? ({ kind: 'ok', value: categories } as CategoriesResponse)),
    ),
    getProduct: vi.fn(),
  };

  TestBed.configureTestingModule({
    providers: [
      provideRouter([
        { path: '', component: RouteStub },
        { path: 'productos', component: RouteStub },
        { path: 'productos/:publicId', component: RouteStub },
      ]),
      { provide: CatalogService, useValue: catalog },
    ],
  });

  const router = TestBed.inject(Router);
  await router.navigateByUrl(options.url ?? '/');

  const fixture = TestBed.createComponent(ProductList);
  await fixture.whenStable();

  return { fixture, host: fixture.nativeElement as HTMLElement, router, catalog };
}

function searchInput(host: HTMLElement): HTMLInputElement {
  return host.querySelector('input[type="search"]') as HTMLInputElement;
}

function type(host: HTMLElement, value: string): void {
  const input = searchInput(host);
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

/** Cierra la ventana del debounce y devuelve el control al reloj real para poder esperar la vista. */
async function settle(fixture: ComponentFixture<ProductList>): Promise<void> {
  vi.advanceTimersByTime(DEBOUNCE_MS);
  vi.useRealTimers();
  await fixture.whenStable();
}

function chipByLabel(host: HTMLElement, label: string): HTMLButtonElement {
  const chips = Array.from(host.querySelectorAll<HTMLButtonElement>('app-category-chip button'));

  return chips.find((chip) => chip.textContent?.includes(label)) as HTMLButtonElement;
}

function lastQuery(catalog: Rendered['catalog']): ProductQuery {
  const calls = catalog.getProducts.mock.calls;

  return calls[calls.length - 1][0] as ProductQuery;
}

describe('ProductList', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('Render_ItemsInThePage_ShowsTheGridAndTheServerTotal', async () => {
    const { host } = await render();

    expect(host.querySelector('[data-testid="product-grid"]')).toBeTruthy();
    expect(host.querySelector('[data-testid="results-summary"]')?.textContent).toContain(
      '16 referencias activas · orden alfabético',
    );
    expect(host.querySelector('[role="status"]')?.textContent).toContain('16 resultados');
  });

  it('Render_EmptyItems_ShowsTheEmptyStateNamingTheFiltersAndNoGrid', async () => {
    const { host } = await render({
      url: '/?search=grapadora&category=papeleria',
      products: () => of<ProductsResponse>({ kind: 'ok', value: paged([]) }),
    });

    expect(host.querySelector('[data-testid="product-grid"]')).toBeNull();

    const empty = host.querySelector('app-empty-state');
    expect(empty).toBeTruthy();
    expect(empty?.textContent).toContain('grapadora');
    expect(empty?.textContent).toContain('Papeleria');
    expect(empty?.textContent).toContain('PRD-');
  });

  it('Render_ErrorResult_ShowsTheErrorStateWithoutAnyHttpCode', async () => {
    const { host } = await render({
      products: () => of<ProductsResponse>({ kind: 'error' }),
    });

    const error = host.querySelector('app-error-state');
    expect(error).toBeTruthy();
    expect(error?.textContent).toContain('No pudimos cargar el catálogo');
    expect(error?.textContent).not.toMatch(/\d{3}/);
    expect(host.querySelector('[data-testid="product-grid"]')).toBeNull();
  });

  it('Click_Retry_RequestsTheProductsAgainKeepingTheFilters', async () => {
    const { host, fixture, catalog } = await render({
      url: '/?category=papeleria',
      products: () => of<ProductsResponse>({ kind: 'error' }),
    });

    expect(catalog.getProducts).toHaveBeenCalledTimes(1);

    host.querySelector<HTMLButtonElement>('[data-testid="retry-button"] button')?.click();
    await fixture.whenStable();

    expect(catalog.getProducts).toHaveBeenCalledTimes(2);
    expect(lastQuery(catalog).category).toBe('papeleria');
  });

  it('Type_AnExactSku_NavigatesToTheDetailWithoutSearchingTheCatalog', async () => {
    const { host, fixture, router, catalog } = await render();
    vi.useFakeTimers();

    type(host, 'PRD-005');
    await settle(fixture);

    expect(router.url).toBe('/productos/PRD-005');
    expect(catalog.getProducts).not.toHaveBeenCalledWith(
      expect.objectContaining({ search: 'PRD-005' }),
    );
    expect(catalog.getProducts).toHaveBeenCalledTimes(1);
  });

  it('Type_APlainTerm_RequestsTheCatalogWithTheTermOnTheFirstPage', async () => {
    const { host, fixture, router, catalog } = await render({ url: '/?page=2' });
    vi.useFakeTimers();

    type(host, 'resma');
    await settle(fixture);

    expect(catalog.getProducts).toHaveBeenCalledWith(
      expect.objectContaining({ search: 'resma', page: 1 }),
    );
    expect(router.url).toBe('/?search=resma');
  });

  it('Type_SeveralKeystrokesWithinTheDebounceWindow_RequestsTheCatalogOnce', async () => {
    const { host, fixture, catalog } = await render();
    expect(catalog.getProducts).toHaveBeenCalledTimes(1);

    vi.useFakeTimers();
    type(host, 'r');
    type(host, 're');
    type(host, 'res');
    type(host, 'resm');
    type(host, 'resma');
    vi.advanceTimersByTime(DEBOUNCE_MS - 1);
    expect(catalog.getProducts).toHaveBeenCalledTimes(1);

    await settle(fixture);

    expect(catalog.getProducts).toHaveBeenCalledTimes(2);
    expect(lastQuery(catalog).search).toBe('resma');
  });

  it('Click_ACategoryChipOnPageThree_ResetsThePageToOne', async () => {
    const { host, fixture, router, catalog } = await render({ url: '/?page=3' });
    expect(lastQuery(catalog).page).toBe(3);

    chipByLabel(host, 'Tecnologia').click();
    await fixture.whenStable();

    expect(lastQuery(catalog).page).toBe(1);
    expect(lastQuery(catalog).category).toBe('tecnologia');
    expect(router.url).toBe('/?category=tecnologia');
  });

  it('Render_RequestInFlight_ShowsSkeletonsAndTheLoadingStatusRegion', async () => {
    const pending = new Subject<ProductsResponse>();
    const { host } = await render({ products: () => pending.asObservable() });

    expect(host.querySelectorAll('app-skeleton-card')).toHaveLength(PAGE_SIZE);
    expect(host.querySelector('[role="status"]')?.getAttribute('aria-live')).toBe('polite');
    expect(host.querySelector('[role="status"]')?.textContent).toContain('Cargando productos');
    expect(host.querySelector('[data-testid="product-grid"]')).toBeNull();
    expect(host.querySelector('[data-testid="results-summary"]')).toBeNull();
  });

  it('Render_CategoriesFailed_HidesTheChipsButKeepsTheList', async () => {
    const { host } = await render({ categories: { kind: 'error' } });

    expect(host.querySelector('[data-testid="category-filters"]')).toBeNull();
    expect(host.querySelector('[data-testid="product-grid"]')).toBeTruthy();
  });

  it('Change_Pagination_WritesThePageOnTheUrlKeepingThePath', async () => {
    const { host, fixture, router, catalog } = await render({ url: '/productos' });

    const next = Array.from(host.querySelectorAll<HTMLButtonElement>('app-pagination button')).find(
      (button) => button.textContent?.trim() === 'Siguiente',
    );
    next?.click();
    await fixture.whenStable();

    expect(router.url).toBe('/productos?page=2');
    expect(lastQuery(catalog).page).toBe(2);
  });

  it('Render_Always_ShowsTheBrandHeaderLinkedToTheRoot', async () => {
    const { host } = await render();
    const brand = host.querySelector('header a') as HTMLAnchorElement;

    expect(brand.textContent?.trim()).toBe('TheOffice');
    expect(brand.getAttribute('href')).toBe('/');
  });
});
