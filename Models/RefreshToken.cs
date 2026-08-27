using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        [Required]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
