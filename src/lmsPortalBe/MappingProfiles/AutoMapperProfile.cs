using AutoMapper;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.Models;

namespace lmsPortalBe.MappingProfiles
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<RegisterRequestDto, ApplicationUser>(MemberList.None)
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));
            CreateMap<CourseSummaryDto, CourseModel>(MemberList.None);
        }
    }
}
