import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

/** Milisegundos de espera antes de dar por terminado lo que el usuario escribe. */
const DEBOUNCE_MS = 300;

/** Contador de instancias: el `for` del label necesita un `id` unico por campo en la pagina. */
let instanceCount = 0;

/**
 * El agrupado vive aqui adentro a proposito: `term` ya sale con el debounce aplicado y sin
 * repetir el ultimo valor, para que quien lo consuma solo escuche y consulte.
 */
@Component({
  selector: 'app-search-field',
  templateUrl: './search-field.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchField {
  readonly value = input('');
  readonly term = output<string>();

  protected readonly inputId = `app-search-field-${(instanceCount += 1)}`;

  private readonly typed = new Subject<string>();

  constructor() {
    this.typed
      .pipe(debounceTime(DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((value) => this.term.emit(value));
  }

  protected onInput(event: Event): void {
    this.typed.next((event.target as HTMLInputElement).value);
  }
}
