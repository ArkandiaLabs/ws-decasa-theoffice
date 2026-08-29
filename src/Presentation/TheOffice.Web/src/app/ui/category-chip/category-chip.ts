import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

const BASE = 'inline-flex min-h-11 items-center gap-1 rounded-xl border px-3 text-ui';
const INACTIVE = 'border-border-strong bg-surface text-text-body hover:bg-surface-muted';
const ACTIVE = 'border-primary-500 bg-primary-100 text-primary-900';

@Component({
  selector: 'app-category-chip',
  imports: [RouterLink],
  templateUrl: './category-chip.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryChip {
  readonly label = input.required<string>();
  readonly active = input(false);
  readonly variant = input<'filter' | 'link'>('filter');
  /** Solo se usa en la variante `link`. */
  readonly linkParams = input<Record<string, string> | null>(null);
  readonly picked = output<void>();
  readonly cleared = output<void>();

  protected readonly classes = computed(() => `${BASE} ${this.active() ? ACTIVE : INACTIVE}`);

  /**
   * El chip activo es su propio boton de "quitar": anidar un boton dentro de otro es marcado
   * invalido, asi que el nombre accesible del chip entero cambia y la `✕` queda decorativa.
   */
  protected readonly ariaLabel = computed(() =>
    this.active() ? `Quitar filtro de ${this.label()}` : null,
  );

  protected onClick(): void {
    if (this.active()) {
      this.cleared.emit();
      return;
    }

    this.picked.emit();
  }
}
