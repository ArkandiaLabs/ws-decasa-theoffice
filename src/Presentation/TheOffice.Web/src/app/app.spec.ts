import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('Create_Always_MountsTheRouterOutlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('router-outlet')).toBeTruthy();
  });

  // La marca vive en el shell, no en el listado: si vuelve a moverse a una pantalla, la ficha
  // de producto se queda sin cabecera y parece rota.
  it('Render_Always_ShowsTheBrandHeaderLinkedToTheRoot', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const brand = fixture.nativeElement.querySelector('header a') as HTMLAnchorElement;

    expect(brand.textContent?.trim()).toBe('TheOffice');
    expect(brand.getAttribute('href')).toBe('/');
  });

  it('Render_Always_HasNoTaglineNextToTheBrand', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('header')?.textContent?.trim()).toBe('TheOffice');
  });
});
