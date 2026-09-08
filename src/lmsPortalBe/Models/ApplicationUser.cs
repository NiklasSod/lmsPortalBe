using Microsoft.AspNetCore.Identity;

namespace lmsPortalBe.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public ICollection<CourseEnrollment> Enrollments { get; set; } = [];
        public ICollection<Submission> Submissions { get; set; } = [];
        public UserProfile? Profile { get; set; }
    }
}
