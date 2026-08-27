import { CommonModule } from "@angular/common";
import { Component, EventEmitter, Input, Output } from "@angular/core";
import { VerifyTestCasesResultDto } from "@models/assignment.model";
import { ButtonModule } from "primeng/button";
import { MessageModule } from "primeng/message";

/**
 * תוצאת הרצת הפתרון לדוגמה מול מקרי הבדיקה.
 *
 * ההבחנה שהפאנל הזה קיים בשבילה: **שגיאת קומפילציה בפתרון של המורה היא לא מקרה בדיקה
 * שנכשל.** כשהפתרון עצמו לא מתקמפל אין בכלל תוצאות להציג, והצגת "כל הבדיקות נכשלו"
 * הייתה שולחת את המורה לחפש טעות בטבלה הלא נכונה. לכן שני מצבים נפרדים לגמרי בתצוגה.
 *
 * כפתור "תיקון" מופיע רק כש-canFix — שגיאת ריצה מחזירה פלט ריק, וכתיבתו כפלט צפוי
 * הייתה הופכת מקרה בדיקה תקין למקרה שמצפה לכלום, בלחיצה אחת ובלי שהמורה תשים לב.
 */
@Component({
  selector: "app-test-verification-panel",
  standalone: true,
  imports: [CommonModule, ButtonModule, MessageModule],
  templateUrl: "./test-verification-panel.component.html",
  styleUrls: ["./test-verification-panel.component.css"],
})
export class TestVerificationPanelComponent {
  @Input() result: VerifyTestCasesResultDto | null = null;

  /** נושא את ה-index של המקרה שיש לכתוב אליו את הערך שרץ בפועל. */
  @Output() fixRequested = new EventEmitter<number>();

  get allPassed(): boolean {
    return (
      !!this.result &&
      !this.result.hasCompileError &&
      this.result.total > 0 &&
      this.result.passed === this.result.total
    );
  }
}
