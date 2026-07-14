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
    HebrewDatePickerComponent,
    HebrewDateValue,
    getHebrewToday,
} from "@components/hebrew-date-picker/hebrew-date-picker.component";
import {
    CreateLessonRequestDto,
    LessonResponseDto,
    UpdateLessonRequestDto,
} from "@models/lesson.model";
import { LessonsService } from "@services/lessons.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputTextModule } from "primeng/inputtext";

@Component({
  selector: "app-lesson-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
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
                <small
                  class="p-error"
                  *ngIf="form.get('name')?.invalid && form.get('name')?.touched"
                >
                  שם השיעור הוא שדה חובה
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
                <small
                  class="p-error"
                  *ngIf="
                    form.get('teacherName')?.invalid &&
                    form.get('teacherName')?.touched
                  "
                >
                  שם המורה הוא שדה חובה
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
  styles: [],
})
export class LessonFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly lessonsService = inject(LessonsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  lessonId: number | null = null;

  constructor() {
    this.form = this.fb.group({
      name: ["", Validators.required],
      subject: ["", Validators.required],
      lessonDate: [getHebrewToday(), Validators.required],
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
          lessonDate: {
            hebrewYear: lesson.hebrewYear,
            hebrewMonth: lesson.hebrewMonth,
            hebrewDay: lesson.hebrewDay,
          } satisfies HebrewDateValue,
          teacherName: lesson.teacherName,
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
      name: formValue.name,
      subject: formValue.subject,
      hebrewYear: lessonDate.hebrewYear,
      hebrewMonth: lessonDate.hebrewMonth,
      hebrewDay: lessonDate.hebrewDay,
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
