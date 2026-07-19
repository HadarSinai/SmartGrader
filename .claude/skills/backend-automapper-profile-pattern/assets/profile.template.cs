using AutoMapper;
using SmartGrader.Application.Dtos.{Entity};
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Mapping
{
    public class {Entity}Profile : Profile
    {
        public {Entity}Profile()
        {
            // Create DTO -> Entity
            CreateMap<Create{Entity}RequestDto, {Entity}>();

            // Update DTO -> Entity (always ignore server-owned / navigation fields)
            CreateMap<Update{Entity}RequestDto, {Entity}>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
                // .ForMember(dest => dest.{NavigationCollection}, opt => opt.Ignore());

            // Entity -> Response DTO
            CreateMap<{Entity}, {Entity}ResponseDto>();
                // .ForMember(dest => dest.{ComputedField},
                //            opt => opt.MapFrom(src => src.{Collection} != null ? src.{Collection}.Count : 0));

            // Only for symmetric value-object DTOs (not full entities):
            // CreateMap<{ValueObjectDto}, {ValueObject}>().ReverseMap();
        }
    }
}

// No DI registration needed: services.AddAutoMapper(assembly) in
// server/Application/DependencyInjection.cs auto-discovers this Profile.
