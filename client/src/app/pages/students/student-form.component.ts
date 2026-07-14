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
import { StudentsService } from "@services/students.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputTextModule } from "primeng/inputtext";
import { PasswordModule } from "primeng/password";
import { AuthService } from "../../services/auth.service";

@Component({
  selector: "app-student-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    ButtonModule,
    ConfirmDialogModule,
    PasswordModule,
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
                <label class="block font-bold mb-2" for="className"
                  >כיתה *</label
                >
                <input
                  pInputText
                  class="w-full"
                  id="className"
                  formControlName="className"
                  placeholder="לדוגמה: י׳1"
                />
                <small
                  class="p-error"
                  *ngIf="
                    form.get('className')?.invalid &&
                    form.get('className')?.touched
                  "
                >
                  כיתה היא שדה חובה
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
                  <label class="block font-bold mb-2" for="accountEmail"
                    >אימייל</label
                  >
                  <input
                    pInputText
                    class="w-full"
                    id="accountEmail"
                    type="email"
                    dir="ltr"
                    formControlName="email"
                    autocomplete="off"
                  />
                  <small
                    class="p-error"
                    *ngIf="
                      form.get('email')?.invalid && form.get('email')?.touched
                    "
                  >
                    נדרש אימייל תקין
                  </small>
                </div>

                <div class="field col-12 md:col-6">
                  <label class="block font-bold mb-2" for="accountPassword"
                    >סיסמה</label
                  >
                  <p-password
                    inputId="accountPassword"
                    formControlName="password"
                    [feedback]="false"
                    [toggleMask]="true"
                    autocomplete="new-password"
                    styleClass="w-full"
                    inputStyleClass="w-full"
                  />
                  <small
                    class="p-error"
                    *ngIf="
                      form.get('password')?.invalid &&
                      form.get('password')?.touched
                    "
                  >
                    נדרשת סיסמה באורך 8 תווים לפחות
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

  constructor() {
    this.form = this.fb.group({
      fullName: ["", Validators.required],
      className: ["", Validators.required],
      email: [""],
      password: [""],
    });

    // Account fields are optional as a pair — filling one requires the other
    this.form.get("email")?.addValidators((control) => {
      const password = this.form?.get("password")?.value;
      if (!control.value && !password) return null;
      return Validators.email(control) || Validators.required(control);
    });
    this.form.get("password")?.addValidators((control) => {
      const email = this.form?.get("email")?.value;
      if (!control.value && !email) return null;
      return Validators.minLength(8)(control) || Validators.required(control);
    });
    this.form
      .get("email")
      ?.valueChanges.subscribe(() =>
        this.form.get("password")?.updateValueAndValidity({ emitEvent: false }),
      );
    this.form
      .get("password")
      ?.valueChanges.subscribe(() =>
        this.form.get("email")?.updateValueAndValidity({ emitEvent: false }),
      );
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEditMode = true;
      this.studentId = parseInt(id, 10);
      this.loadStudent(this.studentId);
    }
  }

  loadStudent(id: number): void {
    this.loading = true;
    this.studentsService.getById(id).subscribe({
      next: (student: StudentResponseDto) => {
        this.form.patchValue({
          fullName: student.fullName,
          className: student.className,
        });
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
    const { fullName, className, email, password } = this.form.value;

    // New student with account fields filled → create student + login account together
    if (!this.isEditMode && email && password) {
      this.authService
        .createStudentAccount({ fullName, className, email, password })
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
                ? "כבר קיים חשבון עם האימייל הזה"
                : "יצירת החשבון נכשלה, נסי שוב";
            this.loading = false;
          },
        });
      return;
    }

    const request = { fullName, className };

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
    const email = this.form.get("email")?.value;
    const password = this.form.get("password")?.value;

    if (!email || !password || !this.studentId) {
      this.form.get("email")?.markAsTouched();
      this.form.get("password")?.markAsTouched();
      this.accountError = "יש למלא אימייל וסיסמה";
      return;
    }

    this.accountLoading = true;
    this.accountError = null;

    this.authService
      .createAccountForStudent(this.studentId, { email, password })
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
              ? "כבר קיים חשבון עם האימייל הזה"
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
