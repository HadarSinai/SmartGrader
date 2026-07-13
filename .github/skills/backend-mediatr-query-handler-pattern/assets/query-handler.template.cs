// 1) server/Application/UseCases/{Entity}/{UseCaseName}/{Name}Query.cs
using MediatR;
using SmartGrader.Application.Dtos.{Entity};

namespace SmartGrader.Application.UseCases.{Entity}.{UseCaseName}
{
    public record {Name}Query(int Id) : IRequest<{Entity}ResponseDto>;
}

// 2) server/Application/UseCases/{Entity}/{UseCaseName}/{Name}Handler.cs
using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.{Entity};
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.{Entity}.{UseCaseName}
{
    public class {Name}Handler : IRequestHandler<{Name}Query, {Entity}ResponseDto>
    {
        private readonly I{Entity}Repository _repository;
        private readonly IMapper _mapper;

        public {Name}Handler(I{Entity}Repository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<{Entity}ResponseDto> Handle({Name}Query request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (entity is null)
                throw new NotFoundException(nameof({Entity}), request.Id);

            return _mapper.Map<{Entity}ResponseDto>(entity);
        }
    }
}

// 3) (optional) server/Application/UseCases/{Entity}/{UseCaseName}/{Name}Validator.cs
using FluentValidation;

namespace SmartGrader.Application.UseCases.{Entity}.{UseCaseName}
{
    public class {Name}Validator : AbstractValidator<{Name}Query>
    {
        public {Name}Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}

// No DI registration needed: MediatR + FluentValidation auto-discover via assembly scan
// in server/Application/DependencyInjection.cs.
