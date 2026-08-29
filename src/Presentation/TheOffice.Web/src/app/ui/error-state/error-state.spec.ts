import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ErrorState } from './error-state';

describe('ErrorState', () => {
  let fixture: ComponentFixture<ErrorState>;

  function retryButton(): HTMLButtonElement | null {
    return fixture.nativeElement.querySelector('[data-testid="retry-button"] button');
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ErrorState] }).compileComponents();

    fixture = TestBed.createComponent(ErrorState);
  });

  it('Render_Defaults_ShowsTitleMessageAndRetryLabel', async () => {
    await fixture.whenStable();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('No pudimos cargar el catálogo');
    expect(text).toContain('Revisa tu conexión o inténtalo de nuevo en un momento.');
    expect(retryButton()?.textContent?.trim()).toBe('Reintentar');
  });

  it('Render_Always_HidesAnyHttpStatusCode', async () => {
    fixture.componentRef.setInput('message', 'El servidor no respondió.');
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).not.toMatch(/\d/);
  });

  it('Click_RetryButton_EmitsRetry', async () => {
    await fixture.whenStable();

    let emitted = 0;
    fixture.componentInstance.retry.subscribe(() => emitted++);

    retryButton()?.click();
    await fixture.whenStable();

    expect(emitted).toBe(1);
  });

  it('Render_Always_AnnouncesItselfAsAnAlert', async () => {
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeTruthy();
  });

  it('Render_CustomLabels_UsesThem', async () => {
    fixture.componentRef.setInput('title', 'No pudimos cargar el producto');
    fixture.componentRef.setInput('retryLabel', 'Volver a intentar');
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('No pudimos cargar el producto');
    expect(retryButton()?.textContent?.trim()).toBe('Volver a intentar');
  });
});
