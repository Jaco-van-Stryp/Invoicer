import { Component } from '@angular/core';

@Component({
  selector: 'copyright',
  imports: [],
  templateUrl: './copyright.html',
})
export class Copyright {
  date = new Date().getFullYear();
}
