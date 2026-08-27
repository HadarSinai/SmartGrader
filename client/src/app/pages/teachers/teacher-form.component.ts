import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  CreateTeacherRequestDto,
  TeacherResponseDto,
  UpdateTeacherRequestDto,
} from "@models/teacher.model";
import { TeachersService } from "@services/teachers.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputTextModule } from "primeng/inputtext";
import { PasswordModule } from "primeng/password";
import { PasswordChecklistComponent } from "../../components/password-checklist/password-checklist.component";
import { NoHebrewDirective } from "../../core/directives/no-hebrew.directive";
import { passwordStrengthValidator } from "../../core/validators/password.validator";

@Component({
  selector: "app-teacher-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    ButtonModule,
    ConfirmDialogModule,
    PasswordModule,
    PasswordChecklistComponent,
    NoHebrewDirective,
  ],
  providers: [ConfirmationService],
  templateUrl: "./teacher-form.component.html",
  styleUrls: ["./teacher-form.component.css"],
})
export class TeacherFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly teachersService = inject(TeachersService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form!: FormGroup;
  newPassword = new FormControl("", [
    Validators.required,
    passwordStrengthValidator,
  ]);

  loading = false;
  isEditMode = false;
  teacherId: number | null = null;
  teacher: TeacherResponseDto | null = null;

  formError: string | null = null;
  passwordError: string | null = null;
  passwordLoading = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    this.isEditMode = !!id;

    // הטופס נבנה לפי המצב ולא נבנה פעם אחת ומושבת בחלקו: בעריכה אין כאן שם משתמש
    // ואין סיסמה, ובקרות מושבתות היו עדיין מחזירות ערכים ל-form.value.
    this.form = this.isEditMode
      ? this.fb.group({
          fullName: ["", Validators.required],
          email: ["", [Validators.required, Validators.email]],
        })
      : this.fb.group({
          fullName: ["", Validators.required],
          username: [
            "",
            [
              Validators.required,
              Validators.minLength(3),
              Validators.maxLength(50),
              Validators.pattern(/^\S+$/),
            ],
          ],
          email: ["", [Validators.required, Validators.email]],
          password: ["", [Validators.required, passwordStrengthValidator]],
        });

    if (id) {
      this.teacherId = parseInt(id, 10);
      this.loadTeacher(this.teacherId);
    }
  }

  loadTeacher(id: number): void {
    this.loading = true;
    this.teachersService.getById(id).subscribe({
      next: (teacher: TeacherResponseDto) => {
        this.teacher = teacher;
        this.form.patchValue({
          fullName: teacher.fullName,
          email: teacher.email ?? "",
        });

        // מורה שנוצרה לפני עמודת המייל מגיעה בלי מייל. השדה ריק וחובה, ולכן הטופס
        // כבר לא תקין — מסמנים אותו כ-touched כדי שההודעה תופיע מיד ולא רק אחרי
        // שהמנהלת תיגע בשדה ותצא ממנו.
        if (!teacher.email) {
          this.form.get("email")?.markAsTouched();
        }

        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת פרטי המורה נכשלה",
        });
        this.loading = false;
      },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.formError = null;

    const operation = this.isEditMode
      ? this.teachersService.update(
          this.teacherId!,
          this.form.value as UpdateTeacherRequestDto,
        )
      : this.teachersService.create(this.form.value as CreateTeacherRequestDto);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: this.isEditMode
            ? "פרטי המורה עודכנו בהצלחה"
            : "חשבון המורה נוצר בהצלחה",
        });
        this.router.navigate(["/teachers"]);
      },
      error: (err: { status?: number }) => {
        this.loading = false;
        // 409 מגיע גם על שם משתמש תפוס וגם על מייל תפוס — ההודעה מכסה את שניהם
        // במקום לנחש איזה מהם התנגש.
        this.formError =
          err?.status === 409
            ? "שם המשתמש או כתובת המייל כבר תפוסים בחשבון אחר"
            : this.isEditMode
              ? "עדכון פרטי המורה נכשל"
              : "יצירת חשבון המורה נכשלה";
      },
    });
  }

  confirmResetPassword(): void {
    if (this.newPassword.invalid || !this.teacherId) {
      this.newPassword.markAsTouched();
      return;
    }

    this.confirmationService.confirm({
      message: `לאפס את הסיסמה של "${this.teacher?.fullName ?? ""}"? הסיסמה הנוכחית שלה תפסיק לעבוד מיד.`,
      header: "אישור איפוס סיסמה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "איפוס",
      rejectLabel: "ביטול",
      accept: () => this.resetPassword(),
    });
  }

  resetPassword(): void {
    this.passwordLoading = true;
    this.passwordError = null;

    this.teachersService
      .resetPassword(this.teacherId!, { newPassword: this.newPassword.value })
      .subscribe({
        next: () => {
          this.passwordLoading = false;
          this.newPassword.reset("");
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "הסיסמה אופסה בהצלחה. יש למסור אותה למורה.",
          });
        },
        error: () => {
          this.passwordLoading = false;
          this.passwordError = "איפוס הסיסמה נכשל, יש לנסות שוב";
        },
      });
  }

  onCancel(): void {
    if (this.form.dirty || this.newPassword.dirty) {
      this.confirmationService.confirm({
        message: "יש לך שינויים שלא נשמרו. לצאת בכל זאת?",
        header: "שינויים שלא נשמרו",
        icon: "pi pi-exclamation-triangle",
        acceptLabel: "יציאה",
        rejectLabel: "ביטול",
        accept: () => this.router.navigate(["/teachers"]),
      });
      return;
    }
    this.router.navigate(["/teachers"]);
  }
}
