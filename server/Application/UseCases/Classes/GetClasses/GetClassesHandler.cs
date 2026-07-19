using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Classes;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Classes.GetClasses
{
    public class GetClassesHandler
        : IRequestHandler<GetClassesQuery, IReadOnlyList<SchoolClassResponseDto>>
    {
        private readonly ISchoolClassRepository _repository;
        private readonly IMapper _mapper;

        public GetClassesHandler(ISchoolClassRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<SchoolClassResponseDto>> Handle(
            GetClassesQuery request,
            CancellationToken cancellationToken)
        {
            var classes = await _repository.GetAllAsync(request.IncludeArchived, cancellationToken);

            return _mapper.Map<IReadOnlyList<SchoolClassResponseDto>>(classes);
        }
    }
}
