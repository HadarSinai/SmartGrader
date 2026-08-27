import { CommonModule } from "@angular/common";
import { Component, OnDestroy, OnInit } from "@angular/core";
import { ActivatedRoute, RouterModule } from "@angular/router";

import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TagModule } from "primeng/tag";

import {
    SUBMISSION_POLL_INTERVAL_MS,
    SubmissionResponseDto,
    SubmissionStatusPresentation,
    statusPresentation,
} from "@models/submission.model";
import { AuthService } from "@services/auth.service";
import { SubmissionsService } from "@services/submissions.service";
import { SubmissionFeedbackPanelComponent } from "@components/submission-feedback-panel/submission-feedback-panel.component";
import { SubmittedCodeComponent } from "@components/submitted-code/submitted-code.component";

@Component({
  selector: "app-my-feedback",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    CardModule,
    TagModule,
    SubmissionFeedbackPanelComponent,
    SubmittedCodeComponent,
  ],
  templateUrl: "./my-feedback.component.html",
  styleUrls: ["./my-feedback.component.css"],
})
export class MyFeedbackComponent implements OnInit, OnDestroy {
  submission: SubmissionResponseDto | null = null;
  lessonId: number | null = null;
  loading = false;

  // ⚠️ אלה החליפו את "אין צורך לעשות דבר — צוות ההוראה מטפל בהגשות שנכשלו.": בשני המצבים
  // האלה יש לתלמידה מה לתקן, והשרת תמיד אפשר לה להגיש מחדש — רק הכפתור היה חסר.
  readonly compilationFailedNote =
    "הקוד לא הצליח להתקמפל, ולכן לא נבדק. אפשר לתקן את השגיאה ולהגיש מחדש.";
  readonly aiFailedNote =
    "הבדיקה לא הושלמה. אפשר לתקן את הקוד ולהגיש מחדש.";
  // ⚠️ הניסוח הזה הוא הלב: זו דחייה ולא ציון נמוך. "אם התרגיל דרש רקורסיה וכתבת לולאה —
  // זה כאילו לא עשית". פנייה בלשון נקבה, בית ספר לבנות.
  readonly requirementsNotMetNote =
    "התרגיל דרש דרך פתרון מסוימת, והקוד שלך לא עמד בה — ולכן הוא לא נבדק מול מקרי הבדיקה ולא ניתן ציון. הפרטים בטבלה למטה. תקני והגישי שוב.";
  readonly improvableNote =
    "הציון עדיין מתחת לסף של התרגיל, ולכן ההגשה פתוחה: אפשר לתקן ולהגיש שוב, בלי הגבלת ניסיונות. הציון של הניסיון האחרון הוא שנחשב.";
  // תקלת תשתית — לא באשמת התלמידה ואין לה מה לתקן, ולכן אין כאן כפתור הגשה מחדש.
  readonly judgeUnavailableNote =
    "אירעה תקלה זמנית במערכת הבדיקה — הקוד שלך לא נבדק, וזו לא בעיה בקוד. צוות ההוראה מטפל בכך.";

  submissionId!: number;
  private pollHandle: ReturnType<typeof setInterval> | null = null;

  /** המראה של סטטוס — מקור אחד לכל המסכים. */
  statusOf(status?: string | null): SubmissionStatusPresentation {
    return statusPresentation(status);
  }

  get statusLabel(): string {
    const status = this.submission?.status;
    return statusPresentation(status).label;
  }

  constructor(
    private route: ActivatedRoute,
    private submissionsService: SubmissionsService,
    private auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.submissionId = Number(
      this.route.snapshot.paramMap.get("submissionId"),
    );
    const lessonIdParam = this.route.snapshot.queryParamMap.get("lessonId");
    this.lessonId = lessonIdParam !== null ? Number(lessonIdParam) : null;
    this.load();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  private load(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.loading = true;
    this.submissionsService.getById(studentId, this.submissionId).subscribe({
      next: (submission: SubmissionResponseDto) => {
        this.submission = submission;
        this.loading = false;
        this.syncPolling(submission.status);
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  private syncPolling(status: string | null): void {
    const shouldPoll = status === "PendingAi" || status === "ProcessingAi";
    if (shouldPoll && !this.pollHandle) {
      this.pollHandle = setInterval(
        () => this.refreshSilently(),
        SUBMISSION_POLL_INTERVAL_MS,
      );
    } else if (!shouldPoll) {
      this.stopPolling();
    }
  }

  private refreshSilently(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.submissionsService.getById(studentId, this.submissionId).subscribe({
      next: (submission: SubmissionResponseDto) => {
        this.submission = submission;
        this.syncPolling(submission.status);
      },
      error: () => {
        // Keep polling silently; a transient error shouldn't stop the loop.
      },
    });
  }

  private stopPolling(): void {
    if (this.pollHandle) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
  }
}
