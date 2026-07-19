using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.GetLessons
{
    // ClassId — סינון אופציונלי למורה; StudentId — סינון מחויב לפי הכיתה של התלמיד (מה-claim)
    public record GetLessonsQuery(int? ClassId = null, int? StudentId = null) : IRequest<IReadOnlyList<LessonResponseDto>>;
}


