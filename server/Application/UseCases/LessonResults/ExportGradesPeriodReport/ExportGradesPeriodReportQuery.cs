using MediatR;

namespace SmartGrader.Application.UseCases.LessonResults.ExportGradesPeriodReport;

public record ExportGradesPeriodReportQuery(
    int FromHebrewYear,
    int FromHebrewMonth,
    int FromHebrewDay,
    int ToHebrewYear,
    int ToHebrewMonth,
    int ToHebrewDay,
    int? TeacherId) : IRequest<byte[]>;
