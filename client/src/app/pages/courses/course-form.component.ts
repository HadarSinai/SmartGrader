import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputTextModule } from "primeng/inputtext";

import {
  CourseResponseDto,
  CreateCourseRequestDto,
  UpdateCourseRequestDto,
} from "@models/course.model";
import { CoursesService } from "@services/courses.service";

@Component({
  selector: "app-course-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    ButtonModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card sg-form-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">
                  {{ isEditMode ? "עריכת מקצוע" : "מקצוע חדש" }}
                </div>
                <div class="sg-h2">לדוגמה: C#, ג׳אווה, אלגוריתמיקה</div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="formgrid grid">
              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="name"
                  >שם המקצוע *</label
                >
                <input
                  pInputText
                  class="w-full"
                  id="name"
                  formControlName="name"
                  placeholder="לדוגמה: C#"
                />
                <small
                  class="p-error"
                  *ngIf="form.get('name')?.invalid && form.get('name')?.touched"
                >
                  שם המקצוע הוא שדה חובה (עד 100 תווים)
                </small>
              </div>
            </div>

            <div class="sg-form-actions">
              <p-button
                label="ביטול"
                severity="secondary"
                [outlined]="true"
                (onClick)="onCancel()"
                type="button"
              ></p-button>
              <p-button
                [label]="isEditMode ? 'שמירה' : 'יצירה'"
                type="submit"
                styleClass="sg-btn-primary"
                [loading]="loading"
                [disabled]="form.invalid"
              ></p-button>
            </div>
          </form>
        </p-card>
      </div>
    </section>

    <p-confirmDialog></p-confirmDialog>
  `,
})
export class CourseFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly coursesService = inject(CoursesService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  courseId: number | null = null;

  constructor() {
    this.form = this.fb.group({
      name: ["", [Validators.required, Validators.maxLength(100)]],
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEditMode = true;
      this.courseId = parseInt(id, 10);
      this.loadCourse(this.courseId);
    }
  }

  loadCourse(id: number): void {
    this.loading = true;
    this.coursesService.getById(id).subscribe({
      next: (course: CourseResponseDto) => {
        this.form.patchValue({ name: course.name });
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת המקצוע נכשלה",
        });
        this.loading = false;
      },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    const request = { name: this.form.value.name };

    const operation = this.isEditMode
      ? this.coursesService.update(
          this.courseId!,
          request as UpdateCourseRequestDto,
        )
      : this.coursesService.create(request as CreateCourseRequestDto);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: this.isEditMode
            ? "המקצוע עודכן בהצלחה"
            : "המקצוע נוצר בהצלחה",
        });
        this.router.navigate(["/courses"]);
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: this.isEditMode ? "עדכון המקצוע נכשל" : "יצירת המקצוע נכשלה",
        });
        this.loading = false;
      },
    });
  }

  onCancel(): void {
    if (this.form.dirty) {
      this.confirmationService.confirm({
        message: "יש לך שינויים שלא נשמרו. לצאת בכל זאת?",
        header: "שינויים שלא נשמרו",
        icon: "pi pi-exclamation-triangle",
        acceptLabel: "יציאה",
        rejectLabel: "ביטול",
        accept: () => this.router.navigate(["/courses"]),
      });
      return;
    }
    this.router.navigate(["/courses"]);
  }
}
