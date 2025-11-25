using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.GetLessonById;
using SmartGrader.Domain.Abstractions;

public class GetLessonByIdHandler
    : IRequestHandler<GetLessonByIdQuery, LessonResponseDto>
{
    private readonly ILessonRepository _repository;
    private readonly IMapper _mapper;

    public GetLessonByIdHandler(
        ILessonRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<LessonResponseDto> Handle(
        GetLessonByIdQuery request,
        CancellationToken cancellationToken)
    {
        var lesson = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (lesson is null)
            throw new NotFoundException("Lesson", request.Id);

        return _mapper.Map<LessonResponseDto>(lesson);
    }
}
