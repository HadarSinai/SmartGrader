using MediatR;
using SmartGrader.Application.Dtos;

namespace SmartGrader.Application.UseCases.LessonResults.GetLessonResult;

public record GetLessonResultQuery(int StudentId, int LessonId) : IRequest<LessonResultResponseDto?>;
