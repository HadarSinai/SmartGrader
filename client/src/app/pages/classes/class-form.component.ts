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
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";

import { ClassesService } from "@services/classes.service";
import {
  getCurrentHebrewYear,
  hebrewYearToGematria,
} from "../../core/utils/hebrew-year";

@Component({
  selector: "app-class-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    DropdownModule,
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
                  {{ isEditMode ? "עריכת כיתה" : "כיתה חדשה" }}
                </div>
                <div class="sg-h2">שם הכיתה ושנת הלימודים שלה</div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="formgrid grid">
              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="name"
                  >שם הכיתה *</label
                >
                <input
                  pInputText
                  class="w-full"
                  id="name"
                  formControlName="name"
                  placeholder="לדוגמה: י׳1"
                />
                <small
                  class="p-error"
                  *ngIf="form.get('name')?.invalid && form.get('name')?.touched"
                >
                  שם הכיתה הוא שדה חובה (עד 50 תווים)
                </small>
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="academicYear"
                  >שנת לימודים *</label
                >
                <p-dropdown
                  inputId="academicYear"
                  styleClass="w-full"
                  [options]="yearOptions"
                  formControlName="academicYear"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="בחירת שנה"
                ></p-dropdown>
                <small
                  class="p-error"
                  *ngIf="
                    form.get('academicYear')?.invalid &&
                    form.get('academicYear')?.touched
                  "
                >
                  שנת לימודים היא שדה חובה
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
})
export class ClassFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly classesService = inject(ClassesService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  classId: number | null = null;

  yearOptions: { label: string; value: number }[] = [];

  constructor() {
    const currentYear = getCurrentHebrewYear();

    // השנה שעברה, הנוכחית ושתיים קדימה
    this.yearOptions = [-1, 0, 1, 2].map((offset) => {
      const year = currentYear + offset;
      return { label: `${hebrewYearToGematria(year)} (${year})`, value: year };
    });

    this.form = this.fb.group({
      name: ["", [Validators.required, Validators.maxLength(50)]],
      academicYear: [currentYear, Validators.required],
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEditMode = true;
      this.classId = parseInt(id, 10);
      this.loadClass(this.classId);
    }
  }

  loadClass(id: number): void {
    this.loading = true;
    this.classesService.getById(id).subscribe({
      next: (schoolClass) => {
        // שנה שלא ברשימת האפשרויות (כיתה ותיקה) — מוסיפים אותה
        if (!this.yearOptions.some((o) => o.value === schoolClass.academicYear)) {
          this.yearOptions = [
            {
              label: `${schoolClass.academicYearHebrew} (${schoolClass.academicYear})`,
              value: schoolClass.academicYear,
            },
            ...this.yearOptions,
          ];
        }

        this.form.patchValue({
          name: schoolClass.name,
          academicYear: schoolClass.academicYear,
        });
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת הכיתה נכשלה",
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
    const request = this.form.value;

    const operation = this.isEditMode
      ? this.classesService.update(this.classId!, request)
      : this.classesService.create(request);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: this.isEditMode
            ? "הכיתה עודכנה בהצלחה"
            : "הכיתה נוצרה בהצלחה",
        });
        this.router.navigate(["/classes"]);
      },
      error: (err: { status?: number }) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail:
            err?.status === 409
              ? "כיתה בשם זה כבר קיימת בשנה זו"
              : this.isEditMode
                ? "עדכון הכיתה נכשל"
                : "יצירת הכיתה נכשלה",
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
        accept: () => this.router.navigate(["/classes"]),
      });
      return;
    }
    this.router.navigate(["/classes"]);
  }
}
