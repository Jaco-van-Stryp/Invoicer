import { Component } from '@angular/core';
import { LottieAnimation } from '../lottie-animation/lottie-animation';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'logo',
  imports: [LottieAnimation],
  templateUrl: './logo.html',
})
export class Logo {}
