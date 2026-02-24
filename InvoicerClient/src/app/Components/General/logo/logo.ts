import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'logo',
  templateUrl: './logo.html',
  styleUrl: './logo.css',
})
export class Logo {}
