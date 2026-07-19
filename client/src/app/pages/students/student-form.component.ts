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
  CreateStudentRequestDto,
  StudentResponseDto,
  UpdateStudentRequestDto,
} from "@models/student.model";
import { SchoolClassResponseDto } from "@models/class.model";
import { ClassesService } from "@services/classes.service";
import { StudentsService } from "@services/students.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { PasswordModule } from "primeng/password";
import { PasswordChecklistComponent } from "../../components/password-checklist/password-checklist.component";
import { NoHebrewDirective } from "../../core/directives/no-hebrew.directive";
import { passwordStrengthValidator } from "../../core/validators/password.validator";
import { AuthService } from "../../services/auth.service";

@Component({
  selector: "app-student-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    DropdownModule,
    ButtonModule,
    ConfirmDialogModule,
    PasswordModule,
    PasswordChecklistComponent,
    NoHebrewDirective,
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
                  {{ isEditMode ? "עריכת סטודנט" : "סטודנט חדש" }}
                </div>
                <div class="sg-h2">פרטי בסיס להצגת סטודנט במערכת</div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="formgrid grid">
              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="fullName"
                  >שם מלא *</label
                >
                <input
                  pInputText
                  class="w-full"
                  id="fullName"
                  formControlName="fullName"
                  placeholder="לדוגמה: נועה כהן"
                />
                <small
                  class="p-error"
                  *ngIf="
                    form.get('fullName')?.invalid &&
                    form.get('fullName')?.touched
                  "
                >
                  שם מלא הוא שדה חובה
                </small>
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="classId">כיתה *</label>
                <p-dropdown
                  inputId="classId"
                  styleClass="w-full"
                  [options]="classOptions"
                  formControlName="classId"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="בחירת כיתה"
                  [filter]="classOptions.length > 7"
                  filterPlaceholder="חיפוש כיתה"
                ></p-dropdown>
                <small
                  class="p-error"
                  *ngIf="
                    form.get('classId')?.invalid && form.get('classId')?.touched
                  "
                >
                  כיתה היא שדה חובה
                </small>
                <small
                  class="block mt-1 text-color-secondary"
                  *ngIf="classOptions.length === 0 && !classesLoading"
                >
                  אין כיתות פעילות — יש ליצור כיתה תחילה במסך הכיתות
                </small>
              </div>
            </div>

            <!-- חשבון התחברות -->
            <div class="sg-account-section" *ngIf="!isEditMode || !hasAccount">
              <div class="sg-account-title">
                <i class="pi pi-user" aria-hidden="true"></i>
                חשבון התחברות
                <span class="sg-account-optional" *ngIf="!isEditMode"
                  >(אופציונלי — מאפשר לתלמיד/ה להתחבר ולהגיש קוד)</span
                >
              </div>

              <div class="formgrid grid">
                <div class="field col-12 md:col-6">
                  <label class="block font-bold mb-2" for="accountUsername"
                    >שם משתמש</label
                  >
                  <input
                    pInputText
                    class="w-full"
                    id="accountUsername"
                    type="text"
                    formControlName="username"
                    autocomplete="off"
                  />
                  <small
                    class="p-error"
                    *ngIf="
                      form.get('username')?.invalid &&
                      form.get('username')?.touched
                    "
                  >
                    נדרש שם משתמש (לפחות 3 תווים, ללא רווחים)
                  </small>
                </div>

                <div class="field col-12 md:col-6">
                  <label class="block font-bold mb-2" for="accountPassword"
                    >סיסמה</label
                  >
                  <p-password
                    sgNoHebrew
                    inputId="accountPassword"
                    formControlName="password"
                    [feedback]="false"
                    [toggleMask]="true"
                    autocomplete="new-password"
                    styleClass="w-full"
                    inputStyleClass="w-full"
                  />
                  <app-password-checklist
                    *ngIf="form.get('password')?.value"
                    [password]="form.get('password')?.value"
                  />
                  <small
                    class="p-error"
                    *ngIf="
                      form.get('password')?.hasError('required') &&
                      form.get('password')?.touched
                    "
                  >
                    נדרשת סיסמה
                  </small>
                </div>
              </div>

              <div class="sg-account-error" *ngIf="accountError" role="alert">
                <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
                <span>{{ accountError }}</span>
              </div>

              <p-button
                *ngIf="isEditMode"
                label="יצירת חשבון התחברות"
                icon="pi pi-user-plus"
                severity="secondary"
                [outlined]="true"
                type="button"
                [loading]="accountLoading"
                (onClick)="createAccount()"
              >
              </p-button>
            </div>

            <div
              class="sg-account-section sg-account-exists"
              *ngIf="isEditMode && hasAccount"
            >
              <i class="pi pi-check-circle" aria-hidden="true"></i>
              לתלמיד/ה יש חשבון התחברות פעיל
            </div>

            <div class="sg-form-actions">
              <p-button
                label="ביטול"
                severity="secondary"
                [outlined]="true"
                (onClick)="onCancel()"
                type="button"
              >
              </p-button>
              <p-button
                [label]="isEditMode ? 'שמירה' : 'יצירה'"
                type="submit"
                styleClass="sg-btn-primary"
                [loading]="loading"
                [disabled]="form.invalid"
              >
              </p-button>
            </div>
          </form>
        </p-card>
      </div>
    </section>

    <p-confirmDialog></p-confirmDialog>
  `,
  styles: [
    `
      .sg-account-section {
        border-top: 1px solid var(--app-border, #e5e0d8);
        margin-top: 1rem;
        padding-top: 1rem;
        margin-bottom: 0.5rem;
      }

      .sg-account-title {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-weight: 700;
        color: var(--app-text-strong);
        margin-bottom: 0.75rem;
      }

      .sg-account-optional {
        font-weight: 400;
        color: var(--app-text-muted, #75695e);
        font-size: var(--text-sm, 0.875rem);
      }

      .sg-account-error {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        color: var(--status-error, #b4552d);
        background: color-mix(
          in srgb,
          var(--status-error, #b4552d) 10%,
          transparent
        );
        border-radius: var(--radius-sm, 6px);
        padding: 0.6rem 0.75rem;
        margin-bottom: 1rem;
        font-size: var(--text-sm, 0.875rem);
      }

      .sg-account-exists {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        color: var(--status-success, #5f7d5a);
      }

      :host ::ng-deep .p-password {
        width: 100%;
      }
    `,
  ],
})
export class StudentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly studentsService = inject(StudentsService);
  private readonly classesService = inject(ClassesService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  studentId: number | null = null;
  hasAccount = false;
  accountLoading = false;
  accountError: string | null = null;

  classOptions: { label: string; value: number }[] = [];
  classesLoading = false;

  constructor() {
    this.form = this.fb.group({
      fullName: ["", Validators.required],
      classId: [null, Validators.required],
      username: [""],
      password: [""],
    });

    // Account fields are optional as a pair — filling one requires the other
    this.form.get("username")?.addValidators((control) => {
      const password = this.form?.get("password")?.value;
      if (!control.value && !password) return null;
      if (!control.value) return { required: true };
      if (control.value.length < 3 || /\s/.test(control.value))
        return { username: true };
      return null;
    });
    this.form.get("password")?.addValidators((control) => {
      const username = this.form?.get("username")?.value;
      if (!control.value && !username) return null;
      return passwordStrengthValidator(control) || Validators.required(control);
    });
    this.form
      .get("username")
      ?.valueChanges.subscribe(() =>
        this.form.get("password")?.updateValueAndValidity({ emitEvent: false }),
      );
    this.form
      .get("password")
      ?.valueChanges.subscribe(() =>
        this.form.get("username")?.updateValueAndValidity({ emitEvent: false }),
      );
  }

  ngOnInit(): void {
    this.loadClasses();

    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEditMode = true;
      this.studentId = parseInt(id, 10);
      this.loadStudent(this.studentId);
    }
  }

  loadClasses(): void {
    this.classesLoading = true;
    this.classesService.getAll().subscribe({
      next: (classes: SchoolClassResponseDto[]) => {
        this.classOptions = classes.map((c) => ({
          label: `${c.name} — ${c.academicYearHebrew}`,
          value: c.id,
        }));
        this.classesLoading = false;
      },
      error: () => {
        this.classesLoading = false;
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת הכיתות נכשלה",
        });
      },
    });
  }

  loadStudent(id: number): void {
    this.loading = true;
    this.studentsService.getById(id).subscribe({
      next: (student: StudentResponseDto) => {
        this.form.patchValue({
          fullName: student.fullName,
          classId: student.classId,
        });

        // תלמיד/ה בכיתה בארכיון — מציגים אותה ברשימה כדי שהטופס לא יישבר
        if (
          student.classIsArchived &&
          !this.classOptions.some((o) => o.value === student.classId)
        ) {
          this.classOptions = [
            {
              label: `${student.className ?? ""} (ארכיון)`,
              value: student.classId,
            },
            ...this.classOptions,
          ];
        }
        this.hasAccount = student.hasAccount;
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת הסטודנט/ית נכשלה",
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
    const { fullName, classId, username, password } = this.form.value;

    // New student with account fields filled → create student + login account together
    if (!this.isEditMode && username && password) {
      this.authService
        .createStudentAccount({ fullName, classId, username, password })
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: "success",
              summary: "בוצע",
              detail: "הסטודנט/ית וחשבון ההתחברות נוצרו בהצלחה",
            });
            this.router.navigate(["/students"]);
          },
          error: (err: { status?: number }) => {
            this.accountError =
              err?.status === 409
                ? "כבר קיים חשבון עם שם המשתמש הזה"
                : "יצירת החשבון נכשלה, נסי שוב";
            this.loading = false;
          },
        });
      return;
    }

    const request = { fullName, classId };

    const operation = this.isEditMode
      ? this.studentsService.update(
          this.studentId!,
          request as UpdateStudentRequestDto,
        )
      : this.studentsService.create(request as CreateStudentRequestDto);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: this.isEditMode
            ? "הסטודנט/ית עודכן/ה בהצלחה"
            : "הסטודנט/ית נוצר/ה בהצלחה",
        });
        this.router.navigate(["/students"]);
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: this.isEditMode
            ? "עדכון הסטודנט/ית נכשל"
            : "יצירת הסטודנט/ית נכשלה",
        });
        this.loading = false;
      },
    });
  }

  createAccount(): void {
    const username = this.form.get("username")?.value;
    const password = this.form.get("password")?.value;

    if (!username || !password || !this.studentId) {
      this.form.get("username")?.markAsTouched();
      this.form.get("password")?.markAsTouched();
      this.accountError = "יש למלא שם משתמש וסיסמה";
      return;
    }

    this.accountLoading = true;
    this.accountError = null;

    this.authService
      .createAccountForStudent(this.studentId, { username, password })
      .subscribe({
        next: () => {
          this.accountLoading = false;
          this.hasAccount = true;
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "חשבון ההתחברות נוצר בהצלחה",
          });
        },
        error: (err: { status?: number }) => {
          this.accountLoading = false;
          this.accountError =
            err?.status === 409
              ? "כבר קיים חשבון עם שם המשתמש הזה"
              : "יצירת החשבון נכשלה, נסי שוב";
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
        accept: () => this.router.navigate(["/students"]),
      });
      return;
    }
    this.router.navigate(["/students"]);
  }
}
