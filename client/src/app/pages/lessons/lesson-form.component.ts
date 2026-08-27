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
  templateUrl: "./lesson-form.component.html",
  styleUrls: ["./lesson-form.component.css"],
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
