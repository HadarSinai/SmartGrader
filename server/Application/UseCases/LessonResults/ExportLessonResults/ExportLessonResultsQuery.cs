using MediatR;

namespace SmartGrader.Application.UseCases.LessonResults.ExportLessonResults;

public record ExportLessonResultsQuery(int LessonId, int? TeacherId) : IRequest<byte[]>;
