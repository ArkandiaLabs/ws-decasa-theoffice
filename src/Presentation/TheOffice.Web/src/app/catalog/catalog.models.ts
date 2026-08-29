/** Espejo de los DTOs de la API v1. camelCase, como llega el JSON. */

export interface Category {
  readonly publicId: string;
  readonly name: string;
  readonly slug: string;
  readonly description: string;
}

export interface ProductListItem {
  readonly publicId: string;
  readonly name: string;
  readonly price: number;
  /** Puede llegar cadena vacia. */
  readonly imageUrl: string;
  readonly stock: number;
  readonly categoryName: string;
  readonly categorySlug: string;
}

export interface ProductDetail {
  readonly publicId: string;
  readonly name: string;
  /** Texto plano, sin HTML. Nunca se pinta con innerHTML. */
  readonly description: string;
  readonly price: number;
  readonly imageUrl: string;
  readonly stock: number;
  /** El detalle si devuelve inactivos; el listado no. */
  readonly isActive: boolean;
  readonly category: Category | null;
}

export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  /** Lo calcula el servidor. Se usa, no se recalcula. */
  readonly totalPages: number;
}

export interface ProductQuery {
  readonly page?: number;
  readonly pageSize?: number;
  /** El `slug` de la categoria (`papeleria`), no el nombre ni el publicId. */
  readonly category?: string;
  readonly search?: string;
}

/**
 * Los tres desenlaces posibles de una peticion, como valores. Un fallo esperado no viaja como
 * excepcion hasta la plantilla: es el mismo criterio del Result pattern del backend.
 *
 * `error` cubre la API caida (`status === 0`, el caso comun en desarrollo) y cualquier otro
 * fallo del servidor. La pantalla nunca muestra el codigo HTTP, asi que no hace falta
 * distinguirlos mas alla del 404, que si tiene mensaje propio.
 */
export type Fetched<T> =
  | { readonly kind: 'ok'; readonly value: T }
  | { readonly kind: 'not-found' }
  | { readonly kind: 'error' };
