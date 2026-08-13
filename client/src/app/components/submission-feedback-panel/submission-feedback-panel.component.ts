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

        <!-- קטגוריות משוב -->
        <p-tabView styleClass="sg-feedback-tabs">
          <p-tabPanel header="מה טוב">
            <ul *ngIf="feedback.good?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.good">{{ item }}</li>
            </ul>
          </p-tabPanel>
          <p-tabPanel header="נכונות">
            <ul *ngIf="feedback.issues?.correctness?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.issues.correctness">{{ item }}</li>
            </ul>
          </p-tabPanel>
          <p-tabPanel header="קריאות">
            <ul *ngIf="feedback.issues?.readability?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.issues.readability">{{ item }}</li>
            </ul>
          </p-tabPanel>
          <p-tabPanel header="יעילות">
            <ul *ngIf="feedback.issues?.performance?.length; else emptyList" class="sg-feedback-list">
              <li *ngFor="let item of feedback.issues.performance">{{ item }}</li>
            </ul>
          </p-tabPanel>
          <p-tabPanel header="שינויים מומלצים">
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
      <div class="sg-label">תוצאות בדיקה ({{ passedCount }}/{{ submission!.testResults.length }})</div>
      <p-table
        [value]="submission!.testResults"
        [rowHover]="true"
        dataKey="input"
        styleClass="sg-table"
        [expandedRowKeys]="expandedRows"
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
        <ng-template pTemplate="body" let-test let-expanded="expanded" let-i="rowIndex">
          <tr>
            <td>
              <button
                type="button"
                class="sg-row-toggle"
                (click)="toggleRow(test)"
                [attr.aria-label]="expandedRows[test.input] ? 'כווץ שורה' : 'הרחב שורה'"
              >
                <i
                  class="pi"
                  [class.pi-chevron-down]="expandedRows[test.input]"
                  [class.pi-chevron-left]="!expandedRows[test.input]"
                ></i>
              </button>
            </td>
            <td>{{ test.input }}</td>
            <td>{{ test.expected }}</td>
            <td>{{ test.actual }}</td>
            <td>
              <p-tag
                [severity]="test.passed ? 'success' : 'danger'"
                [value]="test.passed ? 'עבר' : 'נכשל'"
                [icon]="test.passed ? 'pi pi-check' : 'pi pi-times'"
              ></p-tag>
            </td>
          </tr>
          <tr *ngIf="expandedRows[test.input]">
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

      .sg-row-toggle {
        background: none;
        border: none;
        cursor: pointer;
        color: var(--app-muted);
        padding: 0.25rem;
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

  expandedRows: Record<string, boolean> = {};

  ngOnChanges(): void {
    // הרחבה כברירת מחדל רק לטסטים שנכשלו — עוברים ניתנים להרחבה לפי דרישה
    const results = this.submission?.testResults ?? [];
    this.expandedRows = results.reduce((acc, t) => {
      if (!t.passed) acc[t.input] = true;
      return acc;
    }, {} as Record<string, boolean>);
  }

  get passedCount(): number {
    return (this.submission?.testResults ?? []).filter((t) => t.passed).length;
  }

  toggleRow(test: { input: string }): void {
    this.expandedRows = {
      ...this.expandedRows,
      [test.input]: !this.expandedRows[test.input],
    };
  }
}
