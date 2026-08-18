using MediatR;
using SmartGrader.Application.Dtos.LessonResults;

namespace SmartGrader.Application.UseCases.LessonResults.GetStudentGradesSummary;

public record GetStudentGradesSummaryQuery(int StudentId, int? TeacherId) : IRequest<StudentGradesSummaryDto>;
