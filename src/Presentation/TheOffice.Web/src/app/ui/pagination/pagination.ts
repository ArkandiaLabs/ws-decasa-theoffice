import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

const CONTROL =
  'inline-flex min-h-11 min-w-11 items-center justify-center rounded-sm border px-3 text-label';
const ENABLED = 'border-secondary bg-surface text-foreground hover:bg-neutral';
const CURRENT = 'border-primary bg-primary text-on-primary';
const DISABLED = 'cursor-not-allowed border-border bg-neutral text-text-muted';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalItems = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly hasItems = computed(() => this.totalItems() > 0);

  /** Las paginas se numeran completas: el catalogo real cabe en dos, no hace falta elipsis. */
  protected readonly pages = computed(() =>
    Array.from({ length: Math.max(this.totalPages(), 0) }, (_, index) => index + 1),
  );

  protected readonly isFirst = computed(() => this.page() <= 1);
  protected readonly isLast = computed(() => this.page() >= this.totalPages());

  /**
   * La leyenda se arma como una sola cadena para que el guion largo y el punto medio lleguen
   * al DOM tal cual, sin que la interpolacion meta espacios de por medio.
   */
  protected readonly legend = computed(() => {
    const first = (this.page() - 1) * this.pageSize() + 1;
    const last = Math.min(this.page() * this.pageSize(), this.totalItems());

    return `Mostrando ${first}–${last} de ${this.totalItems()} · página ${this.page()} de ${this.totalPages()}`;
  });

  protected controlClasses(disabled: boolean): string {
    return `${CONTROL} ${disabled ? DISABLED : ENABLED}`;
  }

  protected pageClasses(target: number): string {
    return `${CONTROL} ${target === this.page() ? CURRENT : ENABLED}`;
  }

  protected go(target: number): void {
    if (target < 1 || target > this.totalPages() || target === this.page()) {
      return;
    }

    this.pageChange.emit(target);
  }
}
