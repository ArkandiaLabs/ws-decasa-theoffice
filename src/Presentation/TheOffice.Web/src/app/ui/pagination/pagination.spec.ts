import { TestBed } from '@angular/core/testing';

import { Pagination } from './pagination';

interface PaginationInputs {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

async function render(inputs: PaginationInputs) {
  const fixture = TestBed.createComponent(Pagination);
  fixture.componentRef.setInput('page', inputs.page);
  fixture.componentRef.setInput('pageSize', inputs.pageSize);
  fixture.componentRef.setInput('totalItems', inputs.totalItems);
  fixture.componentRef.setInput('totalPages', inputs.totalPages);
  await fixture.whenStable();

  return { fixture, host: fixture.nativeElement as HTMLElement };
}

function buttonNamed(host: HTMLElement, text: string): HTMLButtonElement {
  const match = Array.from(host.querySelectorAll('button')).find(
    (candidate) => candidate.textContent?.trim() === text,
  );
  if (!match) {
    throw new Error(`No hay un boton rotulado "${text}"`);
  }

  return match;
}

describe('Pagination', () => {
  it('Render_NoItems_DrawsNothing', async () => {
    const { host } = await render({ page: 1, pageSize: 10, totalItems: 0, totalPages: 0 });

    expect(host.querySelector('nav')).toBeNull();
    expect(host.textContent?.trim()).toBe('');
  });

  it('Render_Always_LabelsTheNavigationLandmark', async () => {
    const { host } = await render({ page: 1, pageSize: 10, totalItems: 16, totalPages: 2 });

    expect(host.querySelector('nav')?.getAttribute('aria-label')).toBe('Paginación');
  });

  it('Render_FirstFullPage_ShowsTheRangeLegend', async () => {
    const { host } = await render({ page: 1, pageSize: 10, totalItems: 16, totalPages: 2 });

    expect(host.querySelector('p')?.textContent?.trim()).toBe(
      'Mostrando 1–10 de 16 · página 1 de 2',
    );
  });

  it('Render_LastPartialPage_ClampsTheUpperBoundToTheTotal', async () => {
    const { host } = await render({ page: 2, pageSize: 10, totalItems: 16, totalPages: 2 });

    expect(host.querySelector('p')?.textContent?.trim()).toBe(
      'Mostrando 11–16 de 16 · página 2 de 2',
    );
  });

  it('Render_FirstPage_DisablesThePreviousButton', async () => {
    const { host } = await render({ page: 1, pageSize: 10, totalItems: 16, totalPages: 2 });

    expect(buttonNamed(host, 'Anterior').disabled).toBe(true);
    expect(buttonNamed(host, 'Siguiente').disabled).toBe(false);
  });

  it('Render_LastPage_DisablesTheNextButton', async () => {
    const { host } = await render({ page: 2, pageSize: 10, totalItems: 16, totalPages: 2 });

    expect(buttonNamed(host, 'Siguiente').disabled).toBe(true);
    expect(buttonNamed(host, 'Anterior').disabled).toBe(false);
  });

  it('Render_Always_MarksTheCurrentPageWithAriaCurrent', async () => {
    const { host } = await render({ page: 2, pageSize: 10, totalItems: 16, totalPages: 2 });

    const current = host.querySelectorAll('[aria-current="page"]');
    expect(current.length).toBe(1);
    expect(current[0].textContent?.trim()).toBe('2');
    expect(buttonNamed(host, '1').hasAttribute('aria-current')).toBe(false);
  });

  it('Render_Always_PaintsOneButtonPerPage', async () => {
    const { host } = await render({ page: 1, pageSize: 10, totalItems: 16, totalPages: 2 });

    expect(buttonNamed(host, '1')).toBeTruthy();
    expect(buttonNamed(host, '2')).toBeTruthy();
  });

  it('Click_NextButton_EmitsTheFollowingPage', async () => {
    const { fixture, host } = await render({
      page: 1,
      pageSize: 10,
      totalItems: 16,
      totalPages: 2,
    });
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((value) => emitted.push(value));

    buttonNamed(host, 'Siguiente').click();

    expect(emitted).toEqual([2]);
  });

  it('Click_PreviousButton_EmitsThePrecedingPage', async () => {
    const { fixture, host } = await render({
      page: 2,
      pageSize: 10,
      totalItems: 16,
      totalPages: 2,
    });
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((value) => emitted.push(value));

    buttonNamed(host, 'Anterior').click();

    expect(emitted).toEqual([1]);
  });

  it('Click_CurrentPageNumber_DoesNotEmit', async () => {
    const { fixture, host } = await render({
      page: 1,
      pageSize: 10,
      totalItems: 16,
      totalPages: 2,
    });
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((value) => emitted.push(value));

    buttonNamed(host, '1').click();

    expect(emitted).toEqual([]);
  });

  it('Click_DisabledEdge_DoesNotEmit', async () => {
    const { fixture, host } = await render({
      page: 1,
      pageSize: 10,
      totalItems: 16,
      totalPages: 2,
    });
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((value) => emitted.push(value));

    buttonNamed(host, 'Anterior').click();

    expect(emitted).toEqual([]);
  });
});
