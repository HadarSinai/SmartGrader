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
  templateUrl: "./submitted-code.component.html",
  styleUrls: ["./submitted-code.component.css"],
})
export class SubmittedCodeComponent {
  @Input() submission: SubmissionResponseDto | null = null;
}
