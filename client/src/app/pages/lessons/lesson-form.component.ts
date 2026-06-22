import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  CreateLessonRequestDto,
  LessonResponseDto,
  UpdateLessonRequestDto,
} from "@models/lesson.model";
import { LessonsService } from "@services/lessons.service";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CalendarModule } from "primeng/calendar";
import { CardModule } from "primeng/card";
import { InputTextModule } from "primeng/inputtext";

@Component({
  selector: "app-lesson-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    CalendarModule,
    ButtonModule,
  ],
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
                  {{ isEditMode ? "עריכת שיעור" : "שיעור חדש" }}
                </div>
                <div class="sg-h2">יצירה/עדכון פרטי שיעור</div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="formgrid grid">
              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="name"
                  >שם שיעור *</label
                >
                <input
                  pInputText
                  class="w-full"
                  id="name"
                  formControlName="name"
                  placeholder="לדוגמה: שיעור פתיחה"
                />
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="subject">נושא *</label>
                <input
                  pInputText
                  class="w-full"
                  id="subject"
                  formControlName="subject"
                  placeholder="לדוגמה: מתמטיקה"
                />
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="lessonDate"
                  >תאריך ושעה *</label
                >
                <p-calendar
                  inputId="lessonDate"
                  styleClass="w-full"
                  formControlName="lessonDate"
                  [showTime]="true"
                  [showSeconds]="false"
                  dateFormat="yy-mm-dd"
                  placeholder="בחרי תאריך ושעה"
                >
                </p-calendar>
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="teacherName"
                  >שם מורה *</label
                >
                <input
                  pInputText
                  class="w-full"
                  id="teacherName"
                  formControlName="teacherName"
                  placeholder="לדוגמה: הדר"
                />
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
  `,
  styles: [],
})
export class LessonFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly lessonsService = inject(LessonsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  lessonId: number | null = null;

  constructor() {
    this.form = this.fb.group({
      name: ["", Validators.required],
      subject: ["", Validators.required],
      lessonDate: [new Date(), Validators.required],
      teacherName: ["", Validators.required],
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEditMode = true;
      this.lessonId = parseInt(id, 10);
      this.loadLesson(this.lessonId);
    }
  }

  loadLesson(id: number): void {
    this.loading = true;
    this.lessonsService.getById(id).subscribe({
      next: (lesson: LessonResponseDto) => {
        this.form.patchValue({
          name: lesson.name,
          subject: lesson.subject,
          lessonDate: new Date(lesson.lessonDate),
          teacherName: lesson.teacherName,
        });
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: "Failed to load lesson",
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
    const formValue = this.form.value;
    const request = {
      name: formValue.name,
      subject: formValue.subject,
      lessonDate: formValue.lessonDate.toISOString(),
      teacherName: formValue.teacherName,
    };

    const operation = this.isEditMode
      ? this.lessonsService.update(
          this.lessonId!,
          request as UpdateLessonRequestDto,
        )
      : this.lessonsService.create(request as CreateLessonRequestDto);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "Success",
          detail: `Lesson ${this.isEditMode ? "updated" : "created"} successfully`,
        });
        this.router.navigate(["/lessons"]);
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: `Failed to ${this.isEditMode ? "update" : "create"} lesson`,
        });
        this.loading = false;
      },
    });
  }

  onCancel(): void {
    this.router.navigate(["/lessons"]);
  }
}
