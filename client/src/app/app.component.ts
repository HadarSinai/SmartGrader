import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { AccessibilityWidgetComponent } from './components/accessibility/accessibility-widget.component';
import { AccessibilityService } from './services/accessibility.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastModule, AccessibilityWidgetComponent],
  templateUrl: "./app.component.html",
})
export class AppComponent {
  constructor(private a11y: AccessibilityService) {
    this.a11y.init();
  }
}
