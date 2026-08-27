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
  template: `
    <p-dialog
      [visible]="visible"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '48rem', maxWidth: '95vw' }"
      [breakpoints]="{ '768px': '95vw' }"
      header="הצעות למקרי בדיקה"
      styleClass="sg-card"
    >
      <div *ngIf="loading" class="sg-suggest-loading">
        <i class="pi pi-spin pi-spinner"></i>
        <span>מכינה הצעות ובודקת אותן מול הפתרון שלך…</span>
      </div>

      <ng-container *ngIf="!loading && result">
        <!-- שורת המצב העליונה. "אומתו מול הפתרון שלך" היא ההבטחה המרכזית של התכונה,
             ולכן היא מוצגת ברמת הרשימה ולא רק פר-שורה. -->
        <div class="sg-suggest-head">
          <div class="font-bold">הצעות ({{ result.cases.length }})</div>
          <p-tag
            *ngIf="result.verified"
            severity="success"
            icon="pi pi-check"
            value="אומתו מול הפתרון שלך"
          ></p-tag>
          <p-tag
            *ngIf="!result.verified"
            severity="warning"
            icon="pi pi-exclamation-triangle"
            value="לא אומתו"
          ></p-tag>
        </div>

        <p-message
          *ngIf="result.warning"
          severity="warn"
          styleClass="w-full mb-3"
          [text]="result.warning"
        ></p-message>

        <div class="flex flex-column gap-2">
          <div
            *ngFor="let item of result.cases; let i = index"
            class="sg-suggest-row"
            [class.sg-suggest-row--flagged]="item.disagreed || !item.verified"
          >
            <p-checkbox
              [inputId]="'suggest' + i"
              [(ngModel)]="selected[i]"
              [binary]="true"
            ></p-checkbox>

            <div class="sg-suggest-body">
              <label [for]="'suggest' + i" class="sg-suggest-values">
                <code>{{ item.input || "(ריק)" }}</code>
                <span class="sg-suggest-arrow">←</span>
                <code>{{ item.expected || "(ריק)" }}</code>
              </label>

              <div class="sg-suggest-meta">
                <span *ngIf="item.why">{{ item.why }}</span>
                <p-tag
                  [severity]="item.isCore ? 'info' : 'secondary'"
                  [value]="item.isCore ? 'מקרה ליבה' : 'מקרה קצה'"
                ></p-tag>
              </div>

              <!-- ⚠️ המחלוקת מוצגת ולא נבלעת: זה מה שמראה למורה שהאימות באמת רץ,
                   ומה שמאפשר לה להבחין בין "המודל טעה" לבין "הפתרון שלי טועה". -->
              <div *ngIf="item.disagreed" class="sg-suggest-note">
                <i class="pi pi-exclamation-triangle"></i>
                ה-AI הציע <code>{{ item.aiExpected || "(ריק)" }}</code
                >, אבל הפתרון שלך החזיר <code>{{ item.expected }}</code
                >. נשמר מה שהפתרון החזיר.
              </div>

              <div
                *ngIf="!item.verified"
                class="sg-suggest-note sg-suggest-note--warn"
              >
                <i class="pi pi-exclamation-triangle"></i>
                לא אומת{{
                  item.verificationError ? " — " + item.verificationError : ""
                }}. כדאי לבדוק את השורה הזו לפני שמירה.
              </div>
            </div>
          </div>
        </div>

        <div *ngIf="result.cases.length === 0" class="sg-hint py-4">
          לא התקבלו הצעות.
        </div>
      </ng-container>

      <ng-template pTemplate="footer">
        <p-button
          label="ביטול"
          severity="secondary"
          [outlined]="true"
          (onClick)="close()"
          type="button"
        ></p-button>
        <p-button
          [label]="addLabel"
          icon="pi pi-plus"
          styleClass="sg-btn-primary"
          [disabled]="loading || selectedCount === 0"
          (onClick)="add()"
          type="button"
        ></p-button>
      </ng-template>
    </p-dialog>
  `,
  styles: [
    `
      .sg-suggest-loading {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        padding: var(--space-6) var(--space-4);
        justify-content: center;
        color: var(--text-color-secondary);
      }

      .sg-suggest-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-3);
        flex-wrap: wrap;
        margin-bottom: var(--space-3);
      }

      .sg-suggest-row {
        display: flex;
        align-items: flex-start;
        gap: var(--space-3);
        padding: var(--space-3);
        border: 1px solid var(--app-border);
        border-radius: var(--radius-lg);
        background: rgba(239, 232, 221, 0.4);
      }

      .sg-suggest-row--flagged {
        border-color: var(--status-warn);
      }

      .sg-suggest-body {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        flex: 1;
        min-width: 0;
      }

      .sg-suggest-values {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        flex-wrap: wrap;
        cursor: pointer;
        font-weight: 600;
      }

      .sg-suggest-values code,
      .sg-suggest-note code {
        background: var(--surface-100);
        border-radius: var(--radius-sm);
        padding: 0 var(--space-2);
        direction: ltr;
        unicode-bidi: embed;
      }

      /* ⚠️ החץ הפוך ל-RTL: "קלט ← פלט" נקרא נכון מימין לשמאל. */
      .sg-suggest-arrow {
        color: var(--text-color-secondary);
      }

      .sg-suggest-meta {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        flex-wrap: wrap;
        font-size: var(--text-sm);
        color: var(--text-color-secondary);
      }

      .sg-suggest-note {
        display: flex;
        align-items: flex-start;
        gap: var(--space-2);
        font-size: var(--text-sm);
        line-height: 1.5;
      }

      .sg-suggest-note--warn {
        color: var(--status-warn-ink);
      }
    `,
  ],
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
