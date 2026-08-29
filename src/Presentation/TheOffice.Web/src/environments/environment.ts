/**
 * Base de la API. En desarrollo es relativa a proposito: `proxy.conf.json` la reenvia a
 * http://localhost:5226 y el navegador nunca ve otro origen. Cero URLs absolutas en el codigo.
 */
export const environment = {
  production: false,
  apiBaseUrl: '/api/v1',
};
