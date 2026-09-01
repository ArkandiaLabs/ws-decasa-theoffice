/**
 * Base de la API. En desarrollo es relativa a proposito: `proxy.conf.json` la reenvia a
 * http://localhost:5226 y el navegador nunca ve otro origen. Cero URLs absolutas en el codigo.
 */
export const environment = {
  production: false,
  apiBaseUrl: '/api/v1',
  // Solo la ficha la usa: v2 es la unica version que devuelve la galeria completa.
  apiV2BaseUrl: '/api/v2',
};
