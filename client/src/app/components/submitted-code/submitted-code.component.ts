import { CommonModule } from "@angular/common";
import { Component, Input } from "@angular/core";

import { SubmissionResponseDto } from "@models/submission.model";

// מרכז את תצוגת הקוד שהוגש במקום אחד, כדי שלא תוכפל בין מסך הסטודנטית (my-feedback)
// למסך המורה (submission-detail) — בדיוק כמו submission-feedback-panel.
//
// 🔴 זה מה שנשבר קודם: שני המסכים הציגו `<pre>{{ submission.sourceCode }}</pre>` בלבד.
// בהגשה רב-קובצית submit-code שולח sourceCode כמחרוזת ריקה במכוון (הקוד נמצא ב-
// sourceFiles), ולכן שני המסכים הראו תיבה ריקה לחלוטין: לא הקוד, לא שמות הקבצים, ולא
// רמז לכך שיש מה להציג. המורה לא יכלה לבדוק הגשה כזו בכלל.
//
// ⚠️ sourceFiles קודם ל-sourceCode ולא ההפך: הגשה רב-קובצית *תמיד* נושאת את שניהם,
// כשה-sourceCode שבה ריק.
@Component({
  selector: "app-submitted-code",
  standalone: true,
  imports: [CommonModule],
  template: `
    <ng-container *ngIf="submission">
      <!-- הגשה רב-קובצית -->
      <ng-container *ngIf="submission.sourceFiles?.length; else singleFile">
        <div class="sg-file" *ngFor="let file of submission.sourceFiles">
          <div class="sg-file__name">
            <i class="pi pi-file" aria-hidden="true"></i>
            {{ file.fileName }}
          </div>
          <pre class="sg-code-box">{{ file.content }}</pre>
        </div>
      </ng-container>

      <!-- הגשה חד-קובצית -->
      <ng-template #singleFile>
        <pre class="sg-code-box">{{ submission.sourceCode }}</pre>
        <!-- ⚠️ בלי המשפט הזה תיבה ריקה נראית זהה לתקלת טעינה. זה קורה כשתרגיל שינה
             את ExpectedFiles אחרי שהוגש, והקבצים נשמרו תחת שמות שכבר לא נדרשים. -->
        <div class="sg-file__empty" *ngIf="!submission.sourceCode">
          לא נשמר קוד בהגשה הזו.
        </div>
      </ng-template>
    </ng-container>
  `,
  styles: [
    `
      .sg-file + .sg-file {
        margin-top: var(--space-3);
      }

      .sg-file__name {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        font-family: var(--app-font-mono);
        font-size: var(--text-sm);
        font-weight: 700;
        margin-bottom: var(--space-1);
        direction: ltr;
        text-align: left;
      }

      .sg-file__empty {
        font-size: var(--text-sm);
        color: var(--app-muted);
      }
    `,
  ],
})
export class SubmittedCodeComponent {
  @Input() submission: SubmissionResponseDto | null = null;
}
