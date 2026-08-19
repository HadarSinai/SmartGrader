using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Lessons.GetLessonById
{
    public class GetLessonByIdHandler
    : IRequestHandler<GetLessonByIdQuery, LessonResponseDto>
    {
        private readonly ILessonRepository _repository;
        private readonly IStudentRepository _studentRepository;
        private readonly IMapper _mapper;

        public GetLessonByIdHandler(
            ILessonRepository repository,
            IStudentRepository studentRepository,
            IMapper mapper)
        {
            _repository = repository;
            _studentRepository = studentRepository;
            _mapper = mapper;
        }

        public async Task<LessonResponseDto> Handle(
                GetLessonByIdQuery request,
                CancellationToken cancellationToken)
        {
            // TeacherId חייב להיות null כשהקורא הוא תלמידה (ר' LessonsController.GetById) —
            // אחרת LessonAccess תזרוק 404 על כל שיעור שאינו "בבעלות" ה-userId של התלמידה עצמה.
            // במקומו מועבר StudentId, וההרשאה נבדקת מול הכיתה שאליה השיעור משויך.
            var lesson = await LessonAccess.GetAccessibleOrThrowAsync(
                _repository, _studentRepository, request.Id, request.TeacherId, request.StudentId, cancellationToken);

            return _mapper.Map<LessonResponseDto>(lesson);
        }
    }
}
