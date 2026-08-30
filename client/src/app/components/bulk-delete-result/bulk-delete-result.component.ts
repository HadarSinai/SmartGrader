import { CommonModule } from "@angular/common";
import { Component, EventEmitter, Input, Output } from "@angular/core";
import { BulkDeleteFailureRow } from "@models/bulk-delete.model";
import { ButtonModule } from "primeng/button";
import { DialogModule } from "primeng/dialog";
import { TableModule } from "primeng/table";

/**
 * מה קרה לכל שורה שנבחרה למחיקה.
 *
 * 🔴 קיים מפני ש**הצלחה חלקית היא התוצאה הרגילה כאן** (B-55): בחירה של עשר שורות שארבע
 * מהן מוגנות מוחקת שש ומסרבת לארבע. הודעה ירוקה של "נמחקו 6" הייתה מסתירה את ארבעת
 * הסירובים, והמורה הייתה חוזרת לחפש למה השורות עדיין שם.
 *
 * ⚠️ רכיב משותף ולא עותק בכל מסך: ארבעה מסכים שמנסחים בעצמם את אותה תוצאה הם ארבע
 * הזדמנויות לנסח אותה אחרת — וזה בדיוק מה שקרה למיפוי הסטטוסים לפני שאוחד.
 *
 * הסיבות עצמן מגיעות מהשרת כלשונן, מהמחיקה הבודדת שסירבה. השם מגיע מהמסך, שמחזיק
 * ממילא את השורות ויודע לתרגם מזהה לשם שהמורה מכירה.
 */
@Component({
  selector: "app-bulk-delete-result",
  standalone: true,
  imports: [CommonModule, DialogModule, TableModule, ButtonModule],
  // בלי styleUrls: הדיאלוג בנוי כולו ממחלקות גלובליות (sg-table, sg-icon-accent),
  // וגיליון ריק היה כלל שהרכיב מגדיר מחדש בלי צורך — ר' ComponentFileLayoutTests.
  templateUrl: "./bulk-delete-result.component.html",
})
export class BulkDeleteResultComponent {
  @Input() visible = false;
  @Input() deletedCount = 0;
  @Input() failures: BulkDeleteFailureRow[] = [];

  /** ⚠️ נדרש כדי ש-[(visible)] במסך המארח יתעדכן גם בסגירה בלחיצה על הרקע או על Esc. */
  @Output() visibleChange = new EventEmitter<boolean>();

  /** «שורה אחת» ולא «1 שורות» — מספר יחיד עם צורת רבים נקרא כמו באג. */
  rowsLabel(count: number): string {
    return count === 1 ? "שורה אחת" : `${count} שורות`;
  }

  onVisibleChange(value: boolean): void {
    this.visible = value;
    this.visibleChange.emit(value);
  }

  close(): void {
    this.onVisibleChange(false);
  }
}
