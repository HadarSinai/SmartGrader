---
name: client-file-download-upload-pattern
description: "Use when downloading files (blob → <a download>) or uploading files (FormData multipart) in the SmartGrader Angular client: responseType 'blob' service methods, the downloadBlob helper, export-button loading states, file-picker + p-dialog import flow with created-count and row-errors table, and toasts. authInterceptor adds the Bearer token to blob requests as-is. USE FOR: 'add an excel/file export button', 'download a file from the API', 'upload a file with FormData', 'build an import dialog with row errors'. NOT for server-side xlsx generation/parsing (see backend-excel-closedxml-pattern) or generic list styling (see client-list-table-pattern)."
---

# Client File Download/Upload Pattern

Blob downloads and multipart uploads through the existing `ApiClient` wrapper. No special auth handling needed — `authInterceptor` attaches the Bearer token to blob/FormData requests automatically.

## When to Use

- Adding an "ייצוא" button that downloads a server-generated file.
- Adding an "ייבוא" dialog that uploads a file and shows per-row errors.
- Reviewing any file transfer code in the client for convention consistency.

## Workflow — Download

1. Service method with `responseType: 'blob'` (from `students.service.ts`):

```typescript
exportExcel(): Observable<Blob> {
  return this.api.http.get(this.api.url('/api/students/export'), {
    responseType: 'blob'
  });
}
```

2. Shared helper [download.ts](../../../client/src/app/core/utils/download.ts):

```typescript
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}
```

3. Component: secondary button with loading state + toasts:

```html
<p-button
  label="ייצוא"
  icon="pi pi-download"
  [outlined]="true"
  styleClass="sg-btn-secondary"
  [loading]="exporting"
  (onClick)="exportExcel()"
></p-button>
```

```typescript
exportExcel(): void {
  this.exporting = true;
  this.studentsService.exportExcel().subscribe({
    next: (blob) => {
      downloadBlob(blob, "students.xlsx");
      this.exporting = false;
      this.messageService.add({ severity: "success", summary: "בוצע", detail: "הקובץ ירד בהצלחה" });
    },
    error: () => {
      this.exporting = false;
      this.messageService.add({ severity: "error", summary: "שגיאה", detail: "הייצוא נכשל" });
    },
  });
}
```

## Workflow — Upload (import dialog)

1. Service method with `FormData` (do NOT set Content-Type manually — the browser sets the multipart boundary):

```typescript
importExcel(file: File): Observable<ImportStudentsResultDto> {
  const formData = new FormData();
  formData.append('file', file, file.name);
  return this.api.http.post<ImportStudentsResultDto>(
    this.api.url('/api/students/import'),
    formData
  );
}
```

2. Result models mirror the server DTO (camelCase): `ImportStudentsResultDto { createdCount, errors }`, `ImportRowErrorDto { rowNumber, message }`.
3. `p-dialog` with a hidden native file input triggered by a button:

```html
<input
  #importFileInput
  type="file"
  accept=".xlsx"
  class="hidden"
  (change)="onImportFileSelected($event)"
/>
<p-button
  label="בחירת קובץ"
  icon="pi pi-file-excel"
  [outlined]="true"
  styleClass="sg-btn-secondary"
  (onClick)="importFileInput.click()"
></p-button>
```

4. On result: show `createdCount` + a `p-table` of row errors (`rowNumber` | `message`), success toast, reload the list; auto-close the dialog only when there are zero errors. Server errors (400/403) surface via `ApiErrorInterceptor` — only reset the `importing` flag in the error callback.
5. Copy: Hebrew-only, gender-neutral; explain the expected columns in the dialog (e.g. שם מלא | כיתה, שורה ראשונה = כותרות).

## Pitfalls

- Don't type the download as `get<Blob>(...)` with a JSON response — you must pass `{ responseType: 'blob' }`.
- Don't set `Content-Type` on FormData posts — it breaks the multipart boundary.
- Don't forget `URL.revokeObjectURL(url)` after triggering the download.
- Don't show duplicate error toasts — `ApiErrorInterceptor` already handles server errors on upload; the export error toast is needed because blob error bodies aren't parsed by the interceptor's message mapping.
- Reset the chosen file + previous result when reopening the dialog.

## Real Files

- [students.service.ts](../../../client/src/app/services/students.service.ts) — export + import methods.
- [lesson-results.service.ts](../../../client/src/app/services/lesson-results.service.ts) — parameterized export.
- [students-list.component.ts](../../../client/src/app/pages/students/students-list.component.ts) / [.html](../../../client/src/app/pages/students/students-list.component.html) — buttons, dialog, result table.
- [lesson-results-list.component.ts](../../../client/src/app/pages/lesson-results/lesson-results-list.component.ts) — card-header export button.

## See Also

- [backend-excel-closedxml-pattern](../backend-excel-closedxml-pattern/SKILL.md) — the server side.
- [client-list-table-pattern](../client-list-table-pattern/SKILL.md) — toolbar/button placement conventions.
