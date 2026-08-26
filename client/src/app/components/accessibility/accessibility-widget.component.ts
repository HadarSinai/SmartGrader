import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarModule } from 'primeng/sidebar';
import { ButtonModule } from 'primeng/button';
import { SliderModule } from 'primeng/slider';
import { InputSwitchModule } from 'primeng/inputswitch';
import { FormsModule } from '@angular/forms';
import {
  AccessibilityService,
  LARGE_TEXT_SCALE,
} from '../../services/accessibility.service';

/**
 * ⚠️ הווידג'ט הזה אינו מחזיק מצב משלו ואינו כותב ל-localStorage. קודם הוא עשה את שניהם:
 * שדות `isDark` / `largeText` / `reduceMotion` משלו, מפתחות גולמיים משלו, ומחלקות CSS
 * משלו — במקביל לשירות שעשה בדיוק את אותו דבר. reduceMotion חי בשני המקומות, ולכן
 * "איפוס" ניקה רק אחד מהם והשני חזר בטעינה הבאה.
 *
 * כל פקד כאן קורא ל-AccessibilityService.update ותו לא.
 */
@Component({
  selector: 'app-accessibility-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarModule, ButtonModule, SliderModule, InputSwitchModule],
  templateUrl: './accessibility-widget.component.html',
  styleUrls: ['./accessibility-widget.component.css']
})
export class AccessibilityWidgetComponent {
  visible = false;

  constructor(public a11y: AccessibilityService) {}

  /** "טקסט גדול" הוא נקודה על סרגל ה-scale, לא מנגנון שני. */
  setLargeText(on: boolean): void {
    this.a11y.update({ scale: on ? LARGE_TEXT_SCALE : 1 });
  }
}
