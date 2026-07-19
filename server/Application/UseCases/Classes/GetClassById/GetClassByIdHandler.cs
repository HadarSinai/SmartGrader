using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Classes;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Classes.GetClassById
{
    public class GetClassByIdHandler
        : IRequestHandler<GetClassByIdQuery, SchoolClassResponseDto>
    {
        private readonly ISchoolClassRepository _repository;
        private readonly IMapper _mapper;

        public GetClassByIdHandler(ISchoolClassRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SchoolClassResponseDto> Handle(
            GetClassByIdQuery request,
            CancellationToken cancellationToken)
        {
            var schoolClass = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (schoolClass is null)
                throw new NotFoundException(nameof(SchoolClass), request.Id);

            return _mapper.Map<SchoolClassResponseDto>(schoolClass);
        }
    }
}
