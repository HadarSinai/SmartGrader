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
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { InputTextModule } from "primeng/inputtext";

@Component({
  selector: "app-student-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
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
  `,
  styles: [],
})
export class StudentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly studentsService = inject(StudentsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  studentId: number | null = null;

  constructor() {
    this.form = this.fb.group({
      fullName: ["", Validators.required],
      className: ["", Validators.required],
    });
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
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: "Failed to load student",
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
    const request = this.form.value;

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
          summary: "Success",
          detail: `Student ${this.isEditMode ? "updated" : "created"} successfully`,
        });
        this.router.navigate(["/students"]);
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: `Failed to ${this.isEditMode ? "update" : "create"} student`,
        });
        this.loading = false;
      },
    });
  }

  onCancel(): void {
    this.router.navigate(["/students"]);
  }
}
