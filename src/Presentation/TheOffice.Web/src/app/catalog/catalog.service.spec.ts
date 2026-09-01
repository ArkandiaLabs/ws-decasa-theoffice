import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../environments/environment';
import { Fetched, PagedResult, ProductDetail, ProductListItem } from './catalog.models';
import { CatalogService, PAGE_SIZE } from './catalog.service';

const BASE = environment.apiBaseUrl;
const BASE_V2 = environment.apiV2BaseUrl;

const listItem: ProductListItem = {
  publicId: 'PRD-001',
  name: 'Resma de papel carta 75g',
  price: 18900,
  imageUrl: 'https://placehold.co/600x400/png?text=Resma',
  stock: 120,
  categoryName: 'Papeleria',
  categorySlug: 'papeleria',
};

const page: PagedResult<ProductListItem> = {
  items: [listItem],
  page: 1,
  pageSize: PAGE_SIZE,
  totalItems: 16,
  totalPages: 2,
};

describe('CatalogService', () => {
  let service: CatalogService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CatalogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GetProducts_NoQuery_SendsPageOneAndTheFrontendPageSize', () => {
    service.getProducts().subscribe();

    const request = httpMock.expectOne((r) => r.url === `${BASE}/products`);
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('pageSize')).toBe(String(PAGE_SIZE));
    expect(request.request.params.has('category')).toBe(false);
    expect(request.request.params.has('search')).toBe(false);
    request.flush(page);
  });

  it('GetProducts_EmptyFilters_OmitsThemFromTheQueryString', () => {
    service.getProducts({ page: 2, category: '', search: '' }).subscribe();

    const request = httpMock.expectOne((r) => r.url === `${BASE}/products`);
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.has('category')).toBe(false);
    expect(request.request.params.has('search')).toBe(false);
    request.flush(page);
  });

  it('GetProducts_CategoryAndSearch_SendsSlugAndTerm', () => {
    service.getProducts({ page: 3, category: 'papeleria', search: 'resma' }).subscribe();

    const request = httpMock.expectOne((r) => r.url === `${BASE}/products`);
    expect(request.request.params.get('category')).toBe('papeleria');
    expect(request.request.params.get('search')).toBe('resma');
    expect(request.request.params.get('page')).toBe('3');
    request.flush(page);
  });

  it('GetProducts_EmptyItems_IsSuccessNotFailure', () => {
    let result: Fetched<PagedResult<ProductListItem>> | undefined;
    service.getProducts({ search: 'no existe' }).subscribe((r) => (result = r));

    const empty: PagedResult<ProductListItem> = {
      items: [],
      page: 1,
      pageSize: PAGE_SIZE,
      totalItems: 0,
      totalPages: 0,
    };
    httpMock.expectOne((r) => r.url === `${BASE}/products`).flush(empty);

    expect(result).toEqual({ kind: 'ok', value: empty });
  });

  it('GetProducts_ApiDown_ReturnsError', () => {
    let result: Fetched<PagedResult<ProductListItem>> | undefined;
    service.getProducts().subscribe((r) => (result = r));

    httpMock
      .expectOne((r) => r.url === `${BASE}/products`)
      .flush(null, { status: 0, statusText: 'Unknown Error' });

    expect(result).toEqual({ kind: 'error' });
  });

  it('GetProduct_ExistingSku_AsksV2WhichIsTheOnlyVersionWithTheGallery', () => {
    let result: Fetched<ProductDetail> | undefined;
    service.getProduct('PRD-001').subscribe((r) => (result = r));

    const detail: ProductDetail = {
      publicId: 'PRD-001',
      name: 'Resma de papel carta 75g',
      description: 'Resma de 500 hojas tamano carta.',
      price: 18900,
      images: [
        { publicId: 'PRD-001-IMG-1', url: '/img/prd-001-1.jpg', sortOrder: 0, isPrimary: true },
        { publicId: 'PRD-001-IMG-2', url: '/img/prd-001-2.jpg', sortOrder: 1, isPrimary: false },
      ],
      stock: 120,
      isActive: true,
      category: {
        publicId: 'CAT-001',
        name: 'Papeleria',
        slug: 'papeleria',
        description: 'Papel, cuadernos y utiles.',
      },
    };
    httpMock.expectOne(`${BASE_V2}/products/PRD-001`).flush(detail);

    expect(result).toEqual({ kind: 'ok', value: detail });
  });

  it('GetProduct_UnknownSku_ReturnsNotFound', () => {
    let result: Fetched<ProductDetail> | undefined;
    service.getProduct('PRD-042').subscribe((r) => (result = r));

    httpMock
      .expectOne(`${BASE_V2}/products/PRD-042`)
      .flush(null, { status: 404, statusText: 'Not Found' });

    expect(result).toEqual({ kind: 'not-found' });
  });

  it('GetProduct_ApiDown_ReturnsError', () => {
    let result: Fetched<ProductDetail> | undefined;
    service.getProduct('PRD-001').subscribe((r) => (result = r));

    httpMock
      .expectOne(`${BASE_V2}/products/PRD-001`)
      .flush(null, { status: 0, statusText: 'Unknown Error' });

    expect(result).toEqual({ kind: 'error' });
  });

  it('GetCategories_FlatArray_IsMappedAsIs', () => {
    let result: Fetched<readonly unknown[]> | undefined;
    service.getCategories().subscribe((r) => (result = r));

    const categories = [
      { publicId: 'CAT-001', name: 'Papeleria', slug: 'papeleria', description: 'Papel.' },
      { publicId: 'CAT-002', name: 'Mobiliario', slug: 'mobiliario', description: 'Sillas.' },
    ];
    httpMock.expectOne(`${BASE}/categories`).flush(categories);

    expect(result).toEqual({ kind: 'ok', value: categories });
  });
});
