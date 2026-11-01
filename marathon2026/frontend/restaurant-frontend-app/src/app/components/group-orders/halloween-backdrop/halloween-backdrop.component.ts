import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Decorative Halloween night sky (moon + bats) behind the group pre-order screen. */
@Component({
  selector: 'app-halloween-backdrop',
  templateUrl: './halloween-backdrop.component.html',
  styleUrl: './halloween-backdrop.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
  host: { 'aria-hidden': 'true' },
})
export class HalloweenBackdropComponent {}
