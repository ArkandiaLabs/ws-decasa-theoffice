export const environment = {
  production: true,
  // Se sirve detras del mismo origen que reenvia /api a la API. Si el despliegue separa los
  // dominios, este es el unico sitio que hay que tocar.
  apiBaseUrl: '/api/v1',
  // Solo la ficha la usa: v2 es la unica version que devuelve la galeria completa.
  apiV2BaseUrl: '/api/v2',
};
