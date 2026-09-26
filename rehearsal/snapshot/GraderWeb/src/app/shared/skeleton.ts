import { Component, input } from '@angular/core';

/** Flat loading placeholder the size of the real content (no animation). */
@Component({
  selector: 'app-skeleton',
  template: '',
  host: {
    class: 'skeleton',
    'aria-hidden': 'true',
    '[style.width]': 'width()',
    '[style.height]': 'height()',
  },
})
export class Skeleton {
  readonly width = input.required<string>();
  readonly height = input.required<string>();
}
