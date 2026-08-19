using MediatR;
using SmartGrader.Application.Dtos.Lessons;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
    // StudentId — מזהה התלמידה מה-claim בלבד (null עבור מורה/מנהלת). בלעדיו הסינון לא רץ כלל
    // עבור תלמידה, כי TeacherId שלה הוא null — וכל שיעור בבית הספר היה נקרא.
    public record GetLessonByIdQuery(int Id, int? TeacherId, int? StudentId = null) : IRequest<LessonResponseDto>;
}
