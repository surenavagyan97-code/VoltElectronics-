import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CartStore } from './core/cart-store';
import { ThemeStore } from './core/theme-store';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App implements OnInit {
  private cart = inject(CartStore);
  private theme = inject(ThemeStore);

  ngOnInit(): void {
    void this.cart.load();
  }
}
