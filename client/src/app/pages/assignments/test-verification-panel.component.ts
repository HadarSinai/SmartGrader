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
  template: `
    <div class="sg-verify" *ngIf="result">
      <!-- מצב א': הפתרון של המורה לא מתקמפל. הבעיה שלה, לא של מקרי הבדיקה. -->
      <ng-container *ngIf="result.hasCompileError">
        <div class="sg-verify-head">
          <i class="pi pi-times-circle text-red-500"></i>
          <span class="font-bold">הפתרון לדוגמה שלך לא עובר קומפילציה</span>
        </div>
        <p class="sg-hint mt-2 mb-2">
          זו שגיאה בקוד שהזנת כפתרון, לא במקרי הבדיקה. אחרי שתתקני אותה אפשר לבדוק שוב.
        </p>
        <pre class="sg-verify-error">{{ result.compileError }}</pre>
      </ng-container>

      <!-- מצב ב': הפתרון רץ. עכשיו אפשר לדבר על מקרי הבדיקה. -->
      <ng-container *ngIf="!result.hasCompileError">
        <div class="sg-verify-head">
          <i
            class="pi"
            [ngClass]="
              allPassed
                ? 'pi-check-circle text-green-500'
                : 'pi-exclamation-circle text-yellow-600'
            "
          ></i>
          <span class="font-bold">
            {{ result.passed }} מתוך {{ result.total }} מקרי הבדיקה תואמים לפתרון שלך
          </span>
        </div>

        <p-message
          *ngIf="allPassed"
          severity="success"
          styleClass="w-full mt-2"
          text="כל מקרי הבדיקה תואמים. התרגיל מוכן לכיתה."
        ></p-message>

        <div class="mt-3 flex flex-column gap-2">
          <div
            *ngFor="let row of result.results"
            class="sg-verify-row"
            [class.sg-verify-row--failed]="!row.passed"
          >
            <i
              class="pi"
              [ngClass]="
                row.passed
                  ? 'pi-check-circle text-green-500'
                  : 'pi-times-circle text-red-500'
              "
            ></i>

            <div class="sg-verify-body">
              <div class="sg-verify-values">
                <span class="font-semibold">בדיקה {{ row.index + 1 }}</span>
                <span class="sg-hint">קלט</span>
                <code>{{ row.input || "(ריק)" }}</code>
                <ng-container *ngIf="row.passed">
                  <span class="sg-verify-arrow">←</span>
                  <code>{{ row.actual }}</code>
                </ng-container>
              </div>

              <div *ngIf="!row.passed" class="sg-verify-diff">
                <span>
                  ציפית <code>{{ row.expected || "(ריק)" }}</code>
                </span>
                <span *ngIf="row.canFix">
                  · הפתרון שלך החזיר <code>{{ row.actual }}</code>
                </span>
                <span *ngIf="!row.canFix && row.error" class="text-red-600">
                  · {{ row.error }}
                </span>
                <span
                  *ngIf="!row.canFix && !row.error && row.statusDescription"
                  class="text-red-600"
                >
                  · {{ row.statusDescription }}
                </span>
              </div>

              <div *ngIf="!row.passed && row.canFix" class="mt-2">
                <p-button
                  [label]="'תיקון ל-' + row.actual"
                  icon="pi pi-wrench"
                  size="small"
                  [text]="true"
                  (onClick)="fixRequested.emit(row.index)"
                  type="button"
                ></p-button>
              </div>
            </div>
          </div>
        </div>
      </ng-container>
    </div>
  `,
  styles: [
    `
      .sg-verify {
        margin-top: var(--space-3);
        padding: var(--space-3);
        border: 1px solid var(--app-border);
        border-radius: var(--radius-lg);
        background: var(--surface-0);
      }

      .sg-verify-head {
        display: flex;
        align-items: center;
        gap: var(--space-2);
      }

      .sg-verify-error {
        margin: 0;
        padding: var(--space-3);
        border-radius: var(--radius-md);
        background: var(--surface-100);
        font-size: var(--text-sm);
        direction: ltr;
        text-align: left;
        white-space: pre-wrap;
        overflow-x: auto;
      }

      .sg-verify-row {
        display: flex;
        align-items: flex-start;
        gap: var(--space-2);
        padding: var(--space-2) var(--space-3);
        border: 1px solid var(--app-border);
        border-radius: var(--radius-md);
      }

      .sg-verify-row--failed {
        border-color: var(--status-error);
        background: rgba(254, 226, 226, 0.35);
      }

      .sg-verify-body {
        flex: 1;
        min-width: 0;
      }

      .sg-verify-values,
      .sg-verify-diff {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        flex-wrap: wrap;
      }

      .sg-verify-diff {
        margin-top: var(--space-1);
        font-size: var(--text-sm);
      }

      .sg-verify-arrow {
        color: var(--text-color-secondary);
      }

      /* ערכי קלט/פלט הם תמיד LTR גם בעמוד עברי — מספר או מחרוזת קוד שנדחפה
         לכיוון RTL נקראת הפוך והמורה משווה מול הערך הלא נכון. */
      .sg-verify-values code,
      .sg-verify-diff code {
        background: var(--surface-100);
        border-radius: var(--radius-sm);
        padding: 0 var(--space-2);
        direction: ltr;
        unicode-bidi: embed;
      }
    `,
  ],
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
