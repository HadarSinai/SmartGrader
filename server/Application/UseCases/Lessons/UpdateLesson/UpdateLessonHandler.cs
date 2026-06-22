using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Lessons.UpdateLesson
{
    public class UpdateLessonHandler
        : IRequestHandler<UpdateLessonCommand, LessonResponseDto>
    {
        private readonly ILessonRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateLessonHandler(
            ILessonRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<LessonResponseDto> Handle(
            UpdateLessonCommand request,
            CancellationToken cancellationToken)
        {
            var lesson = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (lesson is null)
                throw new NotFoundException("Lesson", request.Id);

            // ⭐ מקצועי יותר — מיפוי DTO → Entity
            _mapper.Map(request.Dto, lesson);

            // ⭐ אין צורך ב-UpdateAsync
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<LessonResponseDto>(lesson);
        }
    }
}
