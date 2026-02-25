import { Component, signal } from '@angular/core';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'copyright',
  imports: [],
  templateUrl: './copyright.html',
})
export class Copyright {
  readonly year = signal(new Date().getFullYear());
}
