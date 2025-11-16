using MediatR;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

public class CompleteLessonHandler
    : IRequestHandler<CompleteLessonCommand, LessonResult>
{
    private readonly ILessonResultRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteLessonHandler(ILessonResultRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LessonResult> Handle(CompleteLessonCommand command, CancellationToken ct)
    {
        var result = await _repository.GetAsync(command.StudentId, command.LessonId, ct)
                     ?? LessonResult.Create(command.StudentId, command.LessonId);

        result.CompleteWith(command.FinalScore);

        if (result.Id == 0)
            await _repository.AddAsync(result, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return result;
    }
}

