using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Logs;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Logs.GetLogs
{
    public class GetLogsHandler : IRequestHandler<GetLogsQuery, IReadOnlyList<LogResponseDto>>
    {
        private readonly ILogRepository _repository;
        private readonly IMapper _mapper;

        public GetLogsHandler(ILogRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<LogResponseDto>> Handle(
            GetLogsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Log> logs = await _repository.GetLatestAsync(request.MaxCount, cancellationToken);

            if (logs == null || logs.Count == 0)
                return Array.Empty<LogResponseDto>();

            var dtoList = _mapper.Map<List<LogResponseDto>>(logs);
            return dtoList.AsReadOnly();
        }
    }
}
