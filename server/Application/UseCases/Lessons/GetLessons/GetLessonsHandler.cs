using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Lessons.GetLessons
{
    public class GetLessonsHandler
        : IRequestHandler<GetLessonsQuery, IReadOnlyList<LessonResponseDto>>
    {
        private readonly ILessonRepository _repository;
        private readonly IMapper _mapper;

        public GetLessonsHandler(ILessonRepository repository,IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<LessonResponseDto>> Handle(
            GetLessonsQuery request,
            CancellationToken cancellationToken)
        {
            var lessons = await _repository.GetAllAsync(cancellationToken);

            // ✅ אם אין שיעורים – מחזירים אוסף ריק, לא null
            if (lessons == null || lessons.Count == 0)
                return Array.Empty<LessonResponseDto>();

            // ✅ נמפה לרשימה, ואז נהפוך אותה ל־ReadOnly
            var dtoList = _mapper.Map<List<LessonResponseDto>>(lessons);

            return dtoList.AsReadOnly();
        }
    }
}
