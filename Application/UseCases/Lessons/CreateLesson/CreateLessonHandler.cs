using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.CreateLesson
{
    public class CreateLessonHandler
        : IRequestHandler<CreateLessonCommand, LessonResponseDto>
    {
        private readonly ILessonRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateLessonHandler(
            ILessonRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<LessonResponseDto> Handle(
            CreateLessonCommand request,
            CancellationToken cancellationToken)
        {
            // בדיקה אופציונלית – להגן מפני null
            if (request.Dto == null)
                throw new ArgumentNullException(nameof(request.Dto));

            var lesson = _mapper.Map<Lesson>(request.Dto);

            await _repository.AddAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<LessonResponseDto>(lesson);
        }
    }
}
