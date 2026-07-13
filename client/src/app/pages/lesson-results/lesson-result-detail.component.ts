import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";

import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TagModule } from "primeng/tag";

import { LessonResultResponseDto } from "@models/lesson-result.model";
import { LessonResultsService } from "@services/lesson-results.service";

@Component({
  selector: "app-lesson-result-detail",
  standalone: true,
  imports: [CommonModule, ButtonModule, CardModule, TagModule],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">התוצאה שלי</div>
                <div class="sg-h2">התקדמות וציון סופי בשיעור</div>
              </div>

              <p-button
                label="חזרה"
                icon="pi pi-arrow-left"
                severity="secondary"
                [outlined]="true"
                (onClick)="navigateBack()"
              ></p-button>
            </div>
          </ng-template>

          <div class="px-4 pb-4" *ngIf="result">
            <div class="grid">
              <div class="col-12 md:col-6">
                <div class="text-xs font-bold text-color-secondary mb-1">
                  התקדמות
                </div>
                <div class="sg-frac" aria-label="התקדמות בתרגילים">
                  <div class="sg-frac-top">
                    {{ result.completedAssignments }}
                  </div>
                  <div class="sg-frac-line"></div>
                  <div class="sg-frac-bot">{{ result.totalAssignments }}</div>
                </div>
              </div>

              <div class="col-12 md:col-6">
                <div class="text-xs font-bold text-color-secondary mb-1">
                  סטטוס
                </div>
                <p-tag
                  *ngIf="result.isComplete"
                  severity="success"
                  value="השיעור הושלם"
                ></p-tag>
                <p-tag
                  *ngIf="
                    !result.isComplete &&
                    result.completedAssignments < result.totalAssignments
                  "
                  severity="info"
                  value="עדיין מגישים תרגילים"
                ></p-tag>
                <p-tag
                  *ngIf="
                    !result.isComplete &&
                    result.completedAssignments >= result.totalAssignments &&
                    result.totalAssignments > 0
                  "
                  severity="warning"
                  value="כל התרגילים הוגשו, ממתין לסיכום המורה"
                ></p-tag>
              </div>

              <div class="col-12 md:col-6" *ngIf="result.isComplete">
                <div class="text-xs font-bold text-color-secondary mb-1">
                  ציון סופי
                </div>
                <div class="text-3xl font-bold" style="color: var(--accent)">
                  {{ result.finalScore ?? "—" }}
                </div>
              </div>
            </div>
          </div>

          <div
            class="flex align-items-center justify-content-center py-6"
            *ngIf="loading"
          >
            <i class="pi pi-spin pi-spinner text-3xl" aria-hidden="true"></i>
          </div>
        </p-card>
      </div>
    </section>
  `,
  styles: [],
})
export class LessonResultDetailComponent implements OnInit {
  private readonly lessonResultsService = inject(LessonResultsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);

  studentId!: number;
  lessonId!: number;
  result: LessonResultResponseDto | null = null;
  loading = false;

  ngOnInit(): void {
    const studentIdParam = this.route.snapshot.paramMap.get("studentId");
    const lessonIdParam = this.route.snapshot.paramMap.get("lessonId");

    if (!studentIdParam || !lessonIdParam) {
      this.router.navigate(["/students"]);
      return;
    }

    this.studentId = Number(studentIdParam);
    this.lessonId = Number(lessonIdParam);
    this.loadResult();
  }

  loadResult(): void {
    this.loading = true;
    this.lessonResultsService
      .getResult(this.studentId, this.lessonId)
      .subscribe({
        next: (data: LessonResultResponseDto) => {
          this.result = data;
          this.loading = false;
        },
        error: () => {
          this.messageService.add({
            severity: "error",
            summary: "שגיאה",
            detail: "טעינת התוצאה נכשלה",
          });
          this.loading = false;
        },
      });
  }

  navigateBack(): void {
    this.router.navigate(["/students", this.studentId, "submissions"]);
  }
}
