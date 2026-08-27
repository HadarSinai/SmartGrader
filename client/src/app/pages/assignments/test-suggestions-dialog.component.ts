import { CommonModule } from "@angular/common";
import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import {
  SuggestTestCasesResultDto,
  SuggestedTestCaseDto,
} from "@models/assignment.model";
import { ButtonModule } from "primeng/button";
import { CheckboxModule } from "primeng/checkbox";
import { DialogModule } from "primeng/dialog";
import { MessageModule } from "primeng/message";
import { TagModule } from "primeng/tag";

/**
 * חלון סקירת ההצעות של ה-AI.
 *
 * ⚠️ זו **רשימת הצעות, לא עריכה**: שום שורה כאן לא נכנסת לטופס עד שהמורה מסמנת ולוחצת
 * "הוספת המסומנות". סגירה או ביטול לא משאירים דבר.
 *
 * שלוש המחלקות שהתצוגה מבחינה ביניהן, ולמה זה חשוב:
 * - **אומת** — ההצעה הורצה מול הפתרון של המורה והתוצאות תאמו. אפשר לסמוך עליה.
 * - **תוקן** — המודל טעה, ההרצה החזירה משהו אחר, ומה שנשמר הוא תוצאת ההרצה. המחלוקת
 *   מוצגת במפורש עם שני הערכים — ההסתרה שלה הייתה מוחקת בדיוק את הראיה שהאימות עבד.
 * - **לא אומת** — אף אחד לא בדק את השורה הזו. זה ניחוש של מודל, והסימון בולט בהתאם.
 */
@Component({
  selector: "app-test-suggestions-dialog",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    ButtonModule,
    CheckboxModule,
    MessageModule,
    TagModule,
  ],
  templateUrl: "./test-suggestions-dialog.component.html",
  styleUrls: ["./test-suggestions-dialog.component.css"],
})
export class TestSuggestionsDialogComponent {
  @Input() visible = false;
  @Input() loading = false;

  @Input() set result(value: SuggestTestCasesResultDto | null) {
    this._result = value;
    // ברירת המחדל: הצעות שאומתו מסומנות, הצעות שלא — לא. אישור גורף של שורות שאיש
    // לא בדק הוא בדיוק מה שהתכונה נועדה למנוע, אז הן דורשות סימון מודע.
    this.selected = (value?.cases ?? []).map((c) => c.verified);
  }
  get result(): SuggestTestCasesResultDto | null {
    return this._result;
  }
  private _result: SuggestTestCasesResultDto | null = null;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() addSelected = new EventEmitter<SuggestedTestCaseDto[]>();

  selected: boolean[] = [];

  get selectedCount(): number {
    return this.selected.filter(Boolean).length;
  }

  get addLabel(): string {
    return this.selectedCount > 0
      ? `הוספת ${this.selectedCount} המסומנות`
      : "הוספת המסומנות";
  }

  onVisibleChange(value: boolean): void {
    this.visible = value;
    this.visibleChange.emit(value);
  }

  close(): void {
    this.onVisibleChange(false);
  }

  add(): void {
    const cases = this._result?.cases ?? [];
    this.addSelected.emit(cases.filter((_, i) => this.selected[i]));
    this.close();
  }
}
