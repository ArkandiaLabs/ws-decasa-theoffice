import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  Category,
  Fetched,
  PagedResult,
  ProductDetail,
  ProductListItem,
  ProductQuery,
} from './catalog.models';

/**
 * Decision del frontend, no del servidor: la API responde 6 por defecto. Se manda siempre
 * explicito para que el tamano de pagina no cambie si el default del backend cambia.
 */
export const PAGE_SIZE = 10;

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  /** La ficha vive en v2: es la unica version que devuelve la galeria completa del producto. */
  private readonly baseV2 = environment.apiV2BaseUrl;

  getProducts(query: ProductQuery = {}): Observable<Fetched<PagedResult<ProductListItem>>> {
    let params = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? PAGE_SIZE);

    // Un `search=` vacio no es lo mismo que no filtrar: se omite.
    if (query.category) {
      params = params.set('category', query.category);
    }
    if (query.search) {
      params = params.set('search', query.search);
    }

    return this.settle(
      this.http.get<PagedResult<ProductListItem>>(`${this.base}/products`, { params }),
    );
  }

  getProduct(publicId: string): Observable<Fetched<ProductDetail>> {
    return this.settle(
      this.http.get<ProductDetail>(`${this.baseV2}/products/${encodeURIComponent(publicId)}`),
    );
  }

  getCategories(): Observable<Fetched<readonly Category[]>> {
    return this.settle(this.http.get<Category[]>(`${this.base}/categories`));
  }

  /** Convierte la excepcion de HttpClient en uno de los tres desenlaces de `Fetched`. */
  private settle<T>(request: Observable<T>): Observable<Fetched<T>> {
    return request.pipe(
      map((value): Fetched<T> => ({ kind: 'ok', value })),
      catchError((failure: HttpErrorResponse) =>
        of<Fetched<T>>(failure.status === 404 ? { kind: 'not-found' } : { kind: 'error' }),
      ),
    );
  }
}
