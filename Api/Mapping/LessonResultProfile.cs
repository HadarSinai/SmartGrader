using SmartGrader.Api.Dtos;
using AutoMapper;
using SmartGrader.Application.UseCases.LessonResults.CompleteLesson;
using SmartGrader.Domain.Entities;

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
