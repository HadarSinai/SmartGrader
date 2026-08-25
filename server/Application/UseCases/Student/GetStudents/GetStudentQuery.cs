using MediatR;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Students.GetStudents
{
    // ⚠️ אין ברירת מחדל ל-TeacherId בכוונה — השמטתו היא שגיאת קומפילציה ולא דליפה שקטה,
    // בדיוק כמו בשאר ה-Queries שנושאות בעלות. null = מנהל/ת.
    public record GetStudentsQuery(bool IncludeArchived, int? TeacherId)
        : IRequest<IReadOnlyList<StudentResponseDto>>;
}
