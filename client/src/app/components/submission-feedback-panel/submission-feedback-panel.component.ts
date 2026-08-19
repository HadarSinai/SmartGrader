import { CommonModule } from "@angular/common";
import { Component, Input } from "@angular/core";

import { PanelModule } from "primeng/panel";
import { TableModule } from "primeng/table";
import { TabViewModule } from "primeng/tabview";
import { TagModule } from "primeng/tag";

import { SubmissionResponseDto } from "@models/submission.model";

// מרכז את כל תצוגת המשוב המובנה (ציונים, קטגוריות, טסטים) במקום אחד, כדי שהלוגיקה
// לא תוכפל בין מסך הסטודנט (my-feedback) למסך המורה (submission-detail).
@Component({
  selector: "app-submission-feedback-panel",
  standalone: true,
  imports: [CommonModule, PanelModule, TableModule, TabViewModule, TagModule],
  template: `
    <div *ngIf="submission?.feedback as feedback" class="sg-feedback-panel">
      <!-- גיבוי: המשוב לא פורש בהצלחה (הגשה ישנה או תשובת AI לא תקינה) -->
      <div *ngIf="!feedback.parseSucceeded" class="sg-note-box">
        {{ feedback.rawResponse || "לא התקבל משוב." }}
      </div>

      <ng-container *ngIf="feedback.parseSucceeded">
        <!-- פירוט ציונים -->
        <div class="sg-score-grid" *ngIf="feedback.scores as scores">
          <div class="sg-score-tile">
            <div class="sg-score-tile__label">ציון טסטים</div>
            <div class="sg-score-tile__value">
              {{ scores.testScore ?? "—" }}
            </div>
          </div>
          <div class="sg-score-tile">
            <div class="sg-score-tile__label">איכות קוד</div>
            <div class="sg-score-tile__value">
              {{ scores.codeQualityScore ?? "—" }}
            </div>
          </div>
          <div class="sg-score-tile">
            <div class="sg-score-tile__label">יעילות</div>
            <div class="sg-score-tile__value">
              {{ scores.efficiencyScore ?? "—" }}
            </div>
          </div>
          <div class="sg-score-tile sg-score-tile--final">
            <div class="sg-score-tile__label">ציון סופי (AI)</div>
            <div class="sg-score-tile__value">
              {{ scores.finalScore ?? "—" }}
            </div>
          </div>
        </div>

        <!-- מבהיר את הפער בין הציון שנרשם להגשה (טסטים בלבד) להערכת ה-AI שמוצגת מעליו,
             כדי שציון 0 לצד "איכות קוד 80" לא ייראה כמו תקלה -->
        <p class="sg-score-note" *ngIf="submission?.feedback?.scores">
          הציון שנרשם להגשה מחושב מתוצאות הבדיקות בלבד. האריחים שלמעלה הם הערכת ה־AI
          ואינם משנים אותו.
        </p>

        <!-- קטגוריות משוב -->
        <!-- מונה בכותרת כל טאב: בלעדיו צריך לפתוח את כל חמשת הטאבים כדי לגלות היכן
             בכלל יש הערות, ורובם בדרך כלל ריקים -->
        <p-tabView styleClass="sg-feedback-tabs">
          <p-tabPanel [header]="'מה טוב' + countSuffix(feedback.good)">
            <ul *ngIf="feedback.good?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.good">{{ item }}</li>
            </ul>
          </p-tabPanel>
          <p-tabPanel [header]="'נכונות' + countSuffix(feedback.issues?.correctness)">
            <ul *ngIf="feedback.issues?.correctness?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.issues.correctness">{{ item }}</li>
            </ul>
          </p-tabPanel>
          <p-tabPanel [header]="'קריאות' + countSuffix(feedback.issues?.readability)">
            <ul *ngIf="feedback.issues?.readability?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.issues.readability">{{ item }}</li>
            </ul>
          </p-tabPanel>
          <p-tabPanel [header]="'יעילות' + countSuffix(feedback.issues?.performance)">
            <ul *ngIf="feedback.issues?.performance?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.issues.performance">{{ item }}</li>
            </ul>
          </p-tabPanel>
          <p-tabPanel [header]="'שינויים מומלצים' + countSuffix(feedback.minimalChanges)">
            <ul *ngIf="feedback.minimalChanges?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.minimalChanges">{{ item }}</li>
            </ul>
          </p-tabPanel>
        </p-tabView>

        <!-- פתרון מלא (אופציונלי) -->
        <p-panel
          *ngIf="feedback.optionalFullSolution"
          header="הצעה לפתרון מלא"
          [toggleable]="true"
          [collapsed]="true"
          styleClass="sg-solution-panel"
        >
          <pre class="sg-code-box">{{ feedback.optionalFullSolution }}</pre>
        </p-panel>
      </ng-container>
    </div>

    <!-- תוצאות טסטים -->
    <div *ngIf="submission?.testResults?.length" class="sg-test-results">
      <!-- ניסוח מילולי במקום "(0/1)": צירוף סוגריים+ספרות+לוכסן מתהפך ויזואלית בהקשר RTL
           ואי אפשר לדעת מהמסך איזה מספר הוא "עברו" ואיזה "סה"כ" -->
      <div class="sg-test-summary">
        <span class="sg-label">תוצאות הבדיקות</span>
        <span class="sg-test-summary__counts">
          <span class="sg-count sg-count--pass">
            <i class="pi pi-check"></i> עברו: {{ passedCount }}
          </span>
          <span class="sg-count sg-count--fail" *ngIf="failedCount > 0">
            <i class="pi pi-times"></i> נכשלו: {{ failedCount }}
          </span>
          <span class="sg-count sg-count--total">מתוך {{ totalCount }}</span>
        </span>
      </div>

      <div
        class="sg-test-bar"
        role="img"
        [attr.aria-label]="'עברו ' + passedCount + ' בדיקות מתוך ' + totalCount"
      >
        <div class="sg-test-bar__fill" [style.width.%]="passedPercent"></div>
      </div>

      <!-- מטלה עם בדיקה אחת נותנת ציון בינארי (0 או 100) בלי שום דירוג ביניים.
           ההערה מסמנת זאת למורה במקום להשאיר את זה מפתיע במסך התלמידה -->
      <div *ngIf="totalCount === 1" class="sg-single-test-note">
        למטלה זו הוגדרה בדיקה אחת בלבד, ולכן הציון יכול להיות רק 0 או 100.
      </div>
      <!-- ⚠️ המפתח הוא אינדקס השורה ולא test.input: שורות מוסתרות מגיעות עם קלט ריק,
           ומפתח משותף היה מרחיב ומכווץ את כולן יחד. -->
      <p-table
        [value]="submission!.testResults"
        [rowHover]="true"
        styleClass="sg-table"
      >
        <ng-template pTemplate="header">
          <tr>
            <th style="width: 3rem"></th>
            <th>קלט</th>
            <th>ציפייה</th>
            <th>תוצאה</th>
            <th style="width: 6rem">סטטוס</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-test let-i="rowIndex">
          <tr>
            <td>
              <!-- לשורה מוסתרת אין מה להרחיב — אין כפתור בכלל, כדי לא להבטיח תוכן שלא קיים -->
              <button
                *ngIf="!test.isHidden"
                type="button"
                class="sg-row-toggle"
                (click)="toggleRow(i)"
                [attr.aria-label]="expandedRows[i] ? 'כווץ שורה' : 'הרחב שורה'"
              >
                <i
                  class="pi"
                  [class.pi-chevron-down]="expandedRows[i]"
                  [class.pi-chevron-left]="!expandedRows[i]"
                ></i>
              </button>
            </td>
            <ng-container *ngIf="!test.isHidden; else hiddenCells">
              <td>{{ test.input }}</td>
              <td>{{ test.expected }}</td>
              <td>{{ test.actual }}</td>
            </ng-container>
            <ng-template #hiddenCells>
              <td colspan="3" class="sg-hidden-test">
                בדיקה {{ i + 1 }} · מוסתרת
              </td>
            </ng-template>
            <td>
              <p-tag
                [severity]="test.passed ? 'success' : 'danger'"
                [value]="test.passed ? 'עבר' : 'נכשל'"
                [icon]="test.passed ? 'pi pi-check' : 'pi pi-times'"
              ></p-tag>
            </td>
          </tr>
          <tr *ngIf="!test.isHidden && expandedRows[i]">
            <td></td>
            <td colspan="4">
              <div class="sg-test-detail">
                <div><strong>קלט:</strong> {{ test.input }}</div>
                <div><strong>ציפייה:</strong> {{ test.expected }}</div>
                <div><strong>תוצאה בפועל:</strong> {{ test.actual || "—" }}</div>
                <div *ngIf="test.error"><strong>שגיאה:</strong> {{ test.error }}</div>
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>
    </div>

    <ng-template #emptyList>
      <div class="sg-empty-note">אין הערות בקטגוריה זו.</div>
    </ng-template>
  `,
  styles: [
    `
      .sg-feedback-panel {
        display: flex;
        flex-direction: column;
        gap: var(--space-4);
      }

      .sg-score-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
        gap: var(--space-3);
      }

      .sg-score-tile {
        padding: var(--space-3);
        border-radius: var(--radius-md);
        border: 1px solid var(--app-border);
        background: var(--app-surface-2);
        text-align: center;
      }

      .sg-score-tile--final {
        border-color: var(--accent);
      }

      .sg-score-tile__label {
        font-size: var(--text-sm);
        color: var(--app-muted);
      }

      .sg-score-tile__value {
        font-size: var(--text-xl);
        font-weight: 800;
        color: var(--accent);
      }

      .sg-score-note {
        margin: 0;
        font-size: var(--text-sm);
        color: var(--app-muted);
        line-height: 1.5;
      }

      .sg-feedback-list {
        margin: 0;
        padding-inline-start: 1.25rem;
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .sg-empty-note {
        color: var(--app-muted);
        font-size: var(--text-sm);
        padding: var(--space-2) 0;
      }

      .sg-test-results {
        margin-top: var(--space-4);
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .sg-test-summary {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-2);
      }

      .sg-test-summary__counts {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--space-2);
      }

      .sg-count {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        font-size: var(--text-sm);
        font-weight: 600;
        padding: 0.15rem 0.6rem;
        border-radius: var(--radius-md);
      }

      .sg-count--pass {
        color: var(--status-success-ink);
        background: var(--status-success-bg);
      }

      .sg-count--fail {
        color: var(--status-error-ink);
        background: var(--status-error-bg);
      }

      .sg-count--total {
        color: var(--app-muted);
      }

      .sg-test-bar {
        height: 6px;
        border-radius: 999px;
        background: var(--status-error-bg);
        overflow: hidden;
      }

      .sg-test-bar__fill {
        height: 100%;
        background: var(--status-success);
        transition: width 0.3s ease;
      }

      .sg-single-test-note {
        font-size: var(--text-sm);
        color: var(--status-warn-ink);
        background: var(--status-warn-bg);
        border-radius: var(--radius-md);
        padding: var(--space-2) var(--space-3);
      }

      .sg-row-toggle {
        background: none;
        border: none;
        cursor: pointer;
        color: var(--app-muted);
        padding: 0.25rem;
      }

      .sg-hidden-test {
        color: var(--app-muted);
        font-style: italic;
      }

      .sg-test-detail {
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
        padding: var(--space-2) 0;
        font-size: var(--text-sm);
        white-space: pre-wrap;
        word-break: break-word;
      }
    `,
  ],
})
export class SubmissionFeedbackPanelComponent {
  @Input() submission: SubmissionResponseDto | null = null;

  expandedRows: Record<number, boolean> = {};

  ngOnChanges(): void {
    // הרחבה כברירת מחדל רק לטסטים שנכשלו — עוברים ניתנים להרחבה לפי דרישה.
    // שורה מוסתרת לעולם לא נפתחת: אין לה תוכן להציג.
    const results = this.submission?.testResults ?? [];
    this.expandedRows = results.reduce((acc, t, i) => {
      if (!t.passed && !t.isHidden) acc[i] = true;
      return acc;
    }, {} as Record<number, boolean>);
  }

  get passedCount(): number {
    return (this.submission?.testResults ?? []).filter((t) => t.passed).length;
  }

  get totalCount(): number {
    return (this.submission?.testResults ?? []).length;
  }

  get failedCount(): number {
    return this.totalCount - this.passedCount;
  }

  get passedPercent(): number {
    return this.totalCount === 0 ? 0 : (this.passedCount / this.totalCount) * 100;
  }

  // מוסיף מונה לכותרת טאב רק כשיש בו הערות, כדי לא לזרוע "(0)" על פני כל הטאבים
  countSuffix(items: unknown[] | null | undefined): string {
    const count = items?.length ?? 0;
    return count > 0 ? ` (${count})` : "";
  }

  toggleRow(index: number): void {
    this.expandedRows = {
      ...this.expandedRows,
      [index]: !this.expandedRows[index],
    };
  }
}
