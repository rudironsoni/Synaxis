using System;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// Junction table for user-team relationships
    /// </summary>
    public class TeamMembership
    {
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        
        public virtual User User { get; set; }
        
        public Guid TeamId { get; set; }
        
        public virtual Team Team { get; set; }
        
        public Guid OrganizationId { get; set; }
        
        public virtual Organization Organization { get; set; }
        
        /// <summary>
        /// Role: admin, member
        /// </summary>
        [Required]
        public string Role { get; set; } = "member";
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        public Guid? InvitedBy { get; set; }
        
        public virtual User Inviter { get; set; }
    }
}
