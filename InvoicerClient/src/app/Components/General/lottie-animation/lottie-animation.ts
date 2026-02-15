import { Component, input, computed } from '@angular/core';
import { AnimationOptions, LottieComponent } from 'ngx-lottie';

@Component({
  selector: 'animation',
  imports: [LottieComponent],
  templateUrl: './lottie-animation.html',
})
export class LottieAnimation {
  animationName = input.required<string>();
  options = computed<AnimationOptions>(() => ({
    path: '/animations/' + this.animationName() + '.json',
    loop: true,
    autoplay: true,
  }));
}
