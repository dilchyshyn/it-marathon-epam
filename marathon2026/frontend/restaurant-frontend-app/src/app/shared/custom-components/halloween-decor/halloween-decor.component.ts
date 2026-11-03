import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Purely decorative Halloween layer over every page: it never takes clicks and is hidden from screen readers. */
@Component({
  selector: 'app-halloween-decor',
  templateUrl: './halloween-decor.component.html',
  styleUrl: './halloween-decor.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
  host: { 'aria-hidden': 'true' },
})
export class HalloweenDecorComponent {}
