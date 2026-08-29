import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

const BASE = 'inline-flex min-h-11 items-center gap-1 rounded-sm border px-3 text-label';
const INACTIVE = 'border-secondary bg-surface text-foreground hover:bg-neutral';
const ACTIVE = 'border-foreground bg-foreground text-on-secondary';

@Component({
  selector: 'app-category-chip',
  imports: [RouterLink],
  templateUrl: './category-chip.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryChip {
  readonly label = input.required<string>();
  readonly active = input(false);
  /**
   * Un chip activo que no representa un filtro puesto — el "Todas" — no se puede quitar:
   * ofrecer "Quitar filtro de Todas" es un control que no hace nada y que el lector de
   * pantalla lee como una accion real.
   */
  readonly clearable = input(true);
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
  protected readonly removable = computed(() => this.active() && this.clearable());

  protected readonly ariaLabel = computed(() =>
    this.removable() ? `Quitar filtro de ${this.label()}` : null,
  );

  protected onClick(): void {
    if (this.active()) {
      if (!this.clearable()) {
        return;
      }

      this.cleared.emit();
      return;
    }

    this.picked.emit();
  }
}
