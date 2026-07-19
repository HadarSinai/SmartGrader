using AutoMapper;
using SmartGrader.Application.Dtos.Logs;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Mapping
{
    public class LogProfile : Profile
    {
        public LogProfile()
        {
            CreateMap<Log, LogResponseDto>();
        }
    }
}
