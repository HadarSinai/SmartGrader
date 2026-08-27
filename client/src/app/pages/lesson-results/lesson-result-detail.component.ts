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
  templateUrl: "./lesson-result-detail.component.html",
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
