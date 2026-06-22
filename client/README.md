# Grading System Frontend

Production-grade Angular 17 frontend for an educational grading system.

## Features

- **Lessons Management**: Create, read, update, and delete lessons
- **Assignments**: Manage assignments nested under lessons with test cases
- **Students**: Track students and their submissions
- **Submissions**: View and manage student code submissions with grading
- **Typed Models**: Strict TypeScript with no `any` types
- **PrimeNG UI**: Professional UI components
- **Toast Notifications**: User feedback for all operations
- **Confirm Dialogs**: Safe deletion with confirmation
- **Error Handling**: Comprehensive error handling and loading states
- **Responsive Design**: Works on all screen sizes

## Tech Stack

- Angular 17 (Standalone Components)
- Angular CLI 17
- PrimeNG 17
- TypeScript (Strict Mode)
- RxJS

## Development

Install dependencies:
```bash
npm install
```

Start dev server:
```bash
npm start
```

Build for production:
```bash
npm run build
```

## Configuration

Update the API base URL in `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  baseUrl: 'http://localhost:5000'
};
```

## Project Structure

```
src/
├── app/
│   ├── components/
│   │   └── layout/          # Layout components (sidebar, topbar)
│   ├── models/              # TypeScript interfaces from OpenAPI
│   ├── services/            # HTTP services for API calls
│   ├── pages/
│   │   ├── lessons/         # Lessons list and form
│   │   ├── assignments/     # Assignments list and form
│   │   ├── students/        # Students list and form
│   │   └── submissions/     # Submissions list, form, and detail
│   ├── app.component.ts     # Root component
│   ├── app.config.ts        # App configuration
│   └── app.routes.ts        # Routing configuration
├── environments/            # Environment configuration
├── main.ts                  # Application entry point
└── styles.css              # Global styles
```

## API Integration

The frontend consumes a RESTful ASP.NET Core Web API with the following endpoints:

- `GET/POST /api/Lessons`
- `GET/PUT/DELETE /api/Lessons/{id}`
- `GET/POST /api/Lessons/{lessonId}/assignments`
- `GET/PUT/DELETE /api/Lessons/{lessonId}/assignments/{assignmentId}`
- `GET/POST /api/Students`
- `GET/PUT/DELETE /api/Students/{id}`
- `GET/POST /api/Students/{studentId}/submissions`
- `GET/PUT/DELETE /api/Students/{studentId}/submissions/{submissionId}`
