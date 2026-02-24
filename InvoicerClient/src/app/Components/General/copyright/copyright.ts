import { Component } from '@angular/core';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'copyright',
  imports: [],
  templateUrl: './copyright.html',
})
export class Copyright {
  date = new Date().getFullYear();
}
