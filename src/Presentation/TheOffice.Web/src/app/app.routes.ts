import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    // El listado vive en dos rutas: la raiz y su nombre propio. La segunda existe para que la
    // miga de pan y el enlace de vuelta tengan una URL que nombrar.
    path: '',
    pathMatch: 'full',
    title: 'TheOffice — Catálogo',
    loadComponent: () => import('./pages/product-list/product-list').then((m) => m.ProductList),
  },
  {
    path: 'productos',
    pathMatch: 'full',
    title: 'TheOffice — Catálogo',
    loadComponent: () => import('./pages/product-list/product-list').then((m) => m.ProductList),
  },
  { path: '**', redirectTo: '' },
];
