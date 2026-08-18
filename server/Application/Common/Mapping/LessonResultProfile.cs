using SmartGrader.Domain.Entities;
using SmartGrader.Application.Dtos;
using SmartGrader.Application.Dtos.LessonResults;
using AutoMapper;
namespace Application.Common.Mapping
{
    public class LessonResultProfile : Profile
    {
        public LessonResultProfile()
        {
            // CompleteLessonRequestDto → CompleteLessonCommand: לא ממופה כאן בכוונה — CompleteLessonCommand
            // הוא record עם פרמטרים positional חובה; נבנה במפורש ב-LessonResultController.Complete.

            CreateMap<LessonResult, LessonResultResponseDto>().ReverseMap();
        }
    }
}
