using AutoMapper;
using SmartGrader.Application.Common.HebrewDate;
using SmartGrader.Application.Dtos.Classes;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Mapping
{
    public class SchoolClassProfile : Profile
    {
        public SchoolClassProfile()
        {
            // Entity → Response DTO
            CreateMap<SchoolClass, SchoolClassResponseDto>()
                .ForMember(dest => dest.AcademicYearHebrew,
                           opt => opt.MapFrom(src => HebrewDateConverter.ToHebrewYearString(src.AcademicYear)))
                .ForMember(dest => dest.StudentsCount,
                           opt => opt.MapFrom(src => src.Students != null ? src.Students.Count : 0));
        }
    }
}
