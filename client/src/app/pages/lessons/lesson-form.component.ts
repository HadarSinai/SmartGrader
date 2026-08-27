import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
    FormBuilder,
    FormGroup,
    FormsModule,
    ReactiveFormsModule,
    Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
    HebrewDatePickerComponent,
    HebrewDateValue,
    getHebrewToday,
} from "@components/hebrew-date-picker/hebrew-date-picker.component";
import { SchoolClassResponseDto } from "@models/class.model";
import { CourseResponseDto } from "@models/course.model";
import {
    CreateLessonRequestDto,
    LessonResponseDto,
    UpdateLessonRequestDto,
} from "@models/lesson.model";
import { ClassesService } from "@services/classes.service";
import { CoursesService } from "@services/courses.service";
import { LessonsService } from "@services/lessons.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DialogModule } from "primeng/dialog";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { MessagesModule } from "primeng/messages";
import { MultiSelectModule } from "primeng/multiselect";
import { TooltipModule } from "primeng/tooltip";

@Component({
  selector: "app-lesson-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TooltipModule,
    CardModule,
    InputTextModule,
    MultiSelectModule,
    DropdownModule,
    DialogModule,
    MessagesModule,
    HebrewDatePickerComponent,
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
                  {{ isEditMode ? "עריכת שיעור" : "שיעור חדש" }}
                </div>
                <div class="sg-h2">יצירה/עדכון פרטי שיעור</div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div
              class="sg-empty-courses mb-3"
              *ngIf="!coursesLoading && courseOptions.length === 0"
            >
              <i class="pi pi-info-circle" aria-hidden="true"></i>
              <span>
                עדיין אין מקצועות. כדי ליצור שיעור צריך קודם מקצוע אחד לפחות.
              </span>
              <p-button
                label="הוספת מקצוע"
                icon="pi pi-plus"
                [text]="true"
                type="button"
                (onClick)="openQuickAddCourse()"
              ></p-button>
            </div>

            <div class="formgrid grid">
              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="courseId"
                  >מקצוע *</label
                >
                <div class="flex gap-2">
                  <p-dropdown
                    inputId="courseId"
                    styleClass="w-full flex-1"
                    [options]="courseOptions"
                    formControlName="courseId"
                    optionLabel="label"
                    optionValue="value"
                    placeholder="בחירת מקצוע"
                    [filter]="courseOptions.length > 7"
                    filterPlaceHolder="חיפוש מקצוע"
                  ></p-dropdown>
                  <p-button
                    icon="pi pi-plus"
                    [outlined]="true"
                    type="button"
                    pTooltip="הוספת מקצוע חדש"
                    tooltipPosition="top"
                    ariaLabel="הוספת מקצוע חדש"
                    (onClick)="openQuickAddCourse()"
                  ></p-button>
                </div>
                <small
                  class="p-error"
                  *ngIf="
                    form.get('courseId')?.invalid &&
                    form.get('courseId')?.touched
                  "
                >
                  יש לבחור מקצוע
                </small>
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
                <small
                  class="p-error"
                  *ngIf="
                    form.get('subject')?.invalid && form.get('subject')?.touched
                  "
                >
                  נושא השיעור הוא שדה חובה
                </small>
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="lessonDate"
                  >תאריך *</label
                >
                <app-hebrew-date-picker
                  formControlName="lessonDate"
                ></app-hebrew-date-picker>
                <small
                  class="p-error"
                  *ngIf="
                    form.get('lessonDate')?.invalid &&
                    form.get('lessonDate')?.touched
                  "
                >
                  תאריך הוא שדה חובה
                </small>
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="classIds"
                  >כיתות *</label
                >
                <p-multiSelect
                  inputId="classIds"
                  styleClass="w-full"
                  [options]="classOptions"
                  formControlName="classIds"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="בחירת כיתות לשיעור"
                  display="chip"
                  [filter]="classOptions.length > 7"
                  filterPlaceHolder="חיפוש כיתה"
                ></p-multiSelect>
                <small
                  class="p-error"
                  *ngIf="
                    form.get('classIds')?.invalid &&
                    form.get('classIds')?.touched
                  "
                >
                  יש לבחור לפחות כיתה אחת
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

    <p-dialog
      header="מקצוע חדש"
      [(visible)]="quickAddVisible"
      [modal]="true"
      [style]="{ width: '24rem' }"
      [dismissableMask]="true"
    >
      <div class="flex flex-column gap-2" dir="rtl">
        <label for="quickCourseName">שם המקצוע *</label>
        <input
          pInputText
          id="quickCourseName"
          class="w-full"
          [(ngModel)]="quickCourseName"
          [ngModelOptions]="{ standalone: true }"
          placeholder="לדוגמה: C#"
          (keyup.enter)="saveQuickCourse()"
        />
      </div>

      <ng-template pTemplate="footer">
        <p-button
          label="ביטול"
          [text]="true"
          (onClick)="quickAddVisible = false"
        ></p-button>
        <p-button
          label="יצירה"
          styleClass="sg-btn-primary"
          [disabled]="!quickCourseName.trim()"
          [loading]="quickAddSaving"
          (onClick)="saveQuickCourse()"
        ></p-button>
      </ng-template>
    </p-dialog>

    <p-confirmDialog></p-confirmDialog>
  `,
  styles: [
    `
      .sg-empty-courses {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.5rem;
        padding: 0.75rem 1rem;
        border-radius: var(--radius-md);
        background: var(--app-surface-2);
        color: var(--app-muted);
      }
    `,
  ],
})
export class LessonFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly lessonsService = inject(LessonsService);
  private readonly classesService = inject(ClassesService);
  private readonly coursesService = inject(CoursesService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  lessonId: number | null = null;

  classOptions: { label: string; value: number }[] = [];
  courseOptions: { label: string; value: number }[] = [];
  coursesLoading = false;

  quickAddVisible = false;
  quickCourseName = "";
  quickAddSaving = false;

  constructor() {
    this.form = this.fb.group({
      courseId: [null as number | null, Validators.required],
      subject: ["", Validators.required],
      lessonDate: [getHebrewToday(), Validators.required],
      classIds: [[] as number[], Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadClasses();
    this.loadCourses();

    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEditMode = true;
      this.lessonId = parseInt(id, 10);
      this.loadLesson(this.lessonId);
    }
  }

  loadClasses(): void {
    this.classesService.getAll().subscribe({
      next: (classes: SchoolClassResponseDto[]) => {
        this.classOptions = classes.map((c) => ({
          label: `${c.name} — ${c.academicYearHebrew}`,
          value: c.id,
        }));
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת הכיתות נכשלה",
        });
      },
    });
  }

  loadCourses(): void {
    this.coursesLoading = true;
    this.coursesService.getAll().subscribe({
      next: (courses: CourseResponseDto[]) => {
        this.courseOptions = courses.map((c) => ({
          label: c.name,
          value: c.id,
        }));
        this.coursesLoading = false;
      },
      error: () => {
        this.coursesLoading = false;
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת המקצועות נכשלה",
        });
      },
    });
  }

  openQuickAddCourse(): void {
    this.quickCourseName = "";
    this.quickAddVisible = true;
  }

  saveQuickCourse(): void {
    const name = this.quickCourseName.trim();
    if (!name || this.quickAddSaving) {
      return;
    }

    this.quickAddSaving = true;
    this.coursesService.create({ name }).subscribe({
      next: (course: CourseResponseDto) => {
        this.courseOptions = [
          ...this.courseOptions,
          { label: course.name, value: course.id },
        ];
        // בחירה אוטומטית של המקצוע החדש — כך שלא צריך לצאת מהטופס
        this.form.patchValue({ courseId: course.id });
        this.form.markAsDirty();
        this.quickAddSaving = false;
        this.quickAddVisible = false;
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "המקצוע נוצר ונבחר",
        });
      },
      error: () => {
        this.quickAddSaving = false;
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "יצירת המקצוע נכשלה",
        });
      },
    });
  }

  loadLesson(id: number): void {
    this.loading = true;
    this.lessonsService.getById(id).subscribe({
      next: (lesson: LessonResponseDto) => {
        this.form.patchValue({
          courseId: lesson.courseId,
          subject: lesson.subject,
          lessonDate: {
            hebrewYear: lesson.hebrewYear,
            hebrewMonth: lesson.hebrewMonth,
            hebrewDay: lesson.hebrewDay,
          } satisfies HebrewDateValue,
          classIds: lesson.classes.map((c) => c.id),
        });
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת השיעור נכשלה",
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
    const lessonDate = formValue.lessonDate as HebrewDateValue;
    const request = {
      courseId: formValue.courseId,
      subject: formValue.subject,
      hebrewYear: lessonDate.hebrewYear,
      hebrewMonth: lessonDate.hebrewMonth,
      hebrewDay: lessonDate.hebrewDay,
      classIds: formValue.classIds,
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
          summary: "בוצע",
          detail: this.isEditMode
            ? "השיעור עודכן בהצלחה"
            : "השיעור נוצר בהצלחה",
        });
        this.router.navigate(["/lessons"]);
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: this.isEditMode ? "עדכון השיעור נכשל" : "יצירת השיעור נכשלה",
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
        accept: () => this.router.navigate(["/lessons"]),
      });
      return;
    }
    this.router.navigate(["/lessons"]);
  }
}
