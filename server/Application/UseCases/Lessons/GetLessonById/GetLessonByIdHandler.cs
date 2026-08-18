using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.GetLessonById;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
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
            // TeacherId חייב להיות null כשהקורא הוא תלמידה (ר' LessonsController.GetById) —
            // אחרת LessonAccess תזרוק 404 על כל שיעור שאינו "בבעלות" ה-userId של התלמידה עצמה.
            var lesson = await LessonAccess.GetOwnedOrThrowAsync(_repository, request.Id, request.TeacherId, cancellationToken);

            return _mapper.Map<LessonResponseDto>(lesson);
        }
    }
}
