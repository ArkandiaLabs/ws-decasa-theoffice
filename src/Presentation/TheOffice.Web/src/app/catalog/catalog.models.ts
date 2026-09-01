/**
 * Espejo de los DTOs de la API. camelCase, como llega el JSON.
 *
 * El listado viene de v1 y la ficha de v2, que es la unica que devuelve la galeria. De la
 * respuesta de v2 se modela lo que la pantalla usa: `variants` llega y se ignora, porque las
 * presentaciones no tienen interfaz y no es este el cambio que se la va a inventar.
 */

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

export interface ProductImage {
  readonly publicId: string;
  /** Puede llegar cadena vacia. */
  readonly url: string;
  readonly sortOrder: number;
  /**
   * La foto que encabeza el producto en el listado. El servidor ordena la galeria por
   * `sortOrder`, no por esta marca, asi que la principal no tiene por que ser la primera.
   */
  readonly isPrimary: boolean;
}

export interface ProductDetail {
  readonly publicId: string;
  readonly name: string;
  /** Texto plano, sin HTML. Nunca se pinta con innerHTML. */
  readonly description: string;
  readonly price: number;
  /** Ya ordenadas por el servidor: `sortOrder` y, en empate, `publicId`. Puede venir vacia. */
  readonly images: readonly ProductImage[];
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
