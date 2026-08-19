using MediatR;
using SmartGrader.Application.Dtos;

namespace SmartGrader.Application.UseCases.LessonResults.GetLessonResult;

// TeacherId — בעלות על השיעור. null = מנהל/ת או תלמידה שקוראת את התוצאה של עצמה
// (הבדיקה שהיא ניגשת לעצמה נעשית בקונטרולר, על פרמטרי ה-route).
public record GetLessonResultQuery(int StudentId, int LessonId, int? TeacherId) : IRequest<LessonResultResponseDto?>;
