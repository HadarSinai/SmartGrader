using AutoMapper;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Domain.Entities;
using Api.Dtos.LessonResults;
using SmartGrader.Api.Dtos;

namespace SmartGrader.Api.Mapping
{
    public class LessonResultProfile : Profile
    {
        public LessonResultProfile()
        {
            CreateMap<CompleteLessonRequestDto, CompleteLessonCommand>();

            CreateMap<LessonResult, LessonResultResponseDto>().ReverseMap();
        }
    }
}
