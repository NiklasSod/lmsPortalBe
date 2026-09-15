using AutoMapper;
using lmsPortalBe.DTOs.Auth;
using lmsPortalBe.DTOs.Course;
using lmsPortalBe.DTOs.Notification;
using lmsPortalBe.DTOs.Resource;
using lmsPortalBe.DTOs.UserProfile;
using lmsPortalBe.Models;

namespace lmsPortalBe.MappingProfiles
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<RegisterRequestDto, ApplicationUser>(MemberList.None)
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));
            CreateMap<CourseModel, CourseSummaryDto>();
            CreateMap<CourseModel, CourseDetailDto>();
            CreateMap<CourseModule, CourseModuleSummaryDto>();
            CreateMap<CourseEnrollment, CourseEnrollmentDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));

            CreateMap<Activity, ActivityDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.ActivityType.ToString()));
            CreateMap<Assignment, AssignmentDto>();
            CreateMap<Assignment, StudentAssignmentDto>()
                .IncludeBase<Assignment, AssignmentDto>()
                .ForMember(dest => dest.LatestSubmissionId, opt => opt.Ignore())
                .ForMember(dest => dest.LatestSubmissionStatus, opt => opt.Ignore())
                .ForMember(dest => dest.LatestFeedback, opt => opt.Ignore());
            CreateMap<Resource, ResourceDto>();
            CreateMap<Submission, SubmissionDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
            CreateMap<UserNotification, NotificationDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Notification.Type.ToString()))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Notification.Title))
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Notification.Body))
                .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.Notification.CourseId))
                .ForMember(dest => dest.ModuleId, opt => opt.MapFrom(src => src.Notification.ModuleId))
                .ForMember(dest => dest.ActivityId, opt => opt.MapFrom(src => src.Notification.ActivityId))
                .ForMember(dest => dest.ResourceId, opt => opt.MapFrom(src => src.Notification.ResourceId))
                .ForMember(dest => dest.SubmissionId, opt => opt.MapFrom(src => src.Notification.SubmissionId))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Notification.CreatedAt));
            CreateMap<UserProfile, UserProfileDto>();
        }
    }
}
