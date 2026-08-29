import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, filter, Subject } from 'rxjs';

/** Milisegundos de espera antes de dar por terminado lo que el usuario escribe. */
const DEBOUNCE_MS = 300;

/** Contador de instancias: el `for` del label necesita un `id` unico por campo en la pagina. */
let instanceCount = 0;

/**
 * El agrupado vive aqui adentro a proposito: `term` ya sale con el debounce aplicado y sin
 * repetir lo que el consumidor ya sabe, para que quien lo consuma solo escuche y consulte.
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
    // Se compara contra `value`, no contra lo ultimo emitido: `value` es lo que el consumidor ya
    // tiene, y tambien cambia por fuera -- limpiar filtros, quitar el chip, atras/adelante. Con un
    // eco privado, volver a escribir el termino que se acaba de limpiar no emitiria nada y la
    // pantalla se quedaria quieta.
    this.typed
      .pipe(
        debounceTime(DEBOUNCE_MS),
        filter((typed) => typed !== this.value()),
        takeUntilDestroyed(),
      )
      .subscribe((typed) => this.term.emit(typed));
  }

  protected onInput(event: Event): void {
    this.typed.next((event.target as HTMLInputElement).value);
  }
}
