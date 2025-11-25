using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Assignments.UpdateAssignment
{
    public class UpdateAssignmentHandler
        : IRequestHandler<UpdateAssignmentCommand, AssignmentResponseDto>
    {
        private readonly IAssignmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateAssignmentHandler(
            IAssignmentRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AssignmentResponseDto> Handle(
            UpdateAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            var assignment = await _repository.GetByIdAsync(request.AssignmentId, cancellationToken);

            if (assignment is null || assignment.LessonId != request.LessonId)
                throw new NotFoundException(nameof(Assignment), request.AssignmentId);

            assignment.Title = request.Dto.Title;
            assignment.Description = request.Dto.Description;
            assignment.IsBonus = request.Dto.IsBonus;
            assignment.BonusValue = request.Dto.BonusValue;

            await _repository.UpdateAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AssignmentResponseDto>(assignment);
        }
    }
}
