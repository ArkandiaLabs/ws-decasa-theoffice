/**
 * Base de la API. En desarrollo es relativa a proposito: `proxy.conf.json` la reenvia a
 * http://localhost:5226 y el navegador nunca ve otro origen. Cero URLs absolutas en el codigo.
 *
 * Conviven dos versiones porque el catalogo las necesita a las dos: el listado se sirve de v1,
 * que ya trae la foto principal derivada, y la ficha se sirve de v2, la unica que devuelve la
 * galeria completa. No es una migracion a medias: v1 sigue congelada y no se le va a pedir nada.
 */
export const environment = {
  production: false,
  apiBaseUrl: '/api/v1',
  apiV2BaseUrl: '/api/v2',
};
