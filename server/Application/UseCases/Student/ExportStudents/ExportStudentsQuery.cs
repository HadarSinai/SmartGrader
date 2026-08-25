using MediatR;

namespace SmartGrader.Application.UseCases.Students.ExportStudents;

// ⚠️ אין ברירת מחדל ל-TeacherId בכוונה — ר' ההערה ב-GetStudentsQuery. null = מנהל/ת.
public record ExportStudentsQuery(int? TeacherId) : IRequest<byte[]>;
