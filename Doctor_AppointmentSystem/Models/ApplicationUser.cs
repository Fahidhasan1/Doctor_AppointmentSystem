using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doctor_AppointmentSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(250)]
        public string? PresentAddress { get; set; }

        [StringLength(250)]
        public string? PermanentAddress { get; set; }

        [StringLength(50)]
        public string? NID { get; set; }

        [StringLength(250)]
        public string? ProfilePicturePath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedDate { get; set; }

        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Who registered this user (Admin or Receptionist). Null = self-registered patient.
        /// </summary>
        public string? RegisteredByUserId { get; set; }

        [ForeignKey(nameof(RegisteredByUserId))]
        public ApplicationUser? RegisteredByUser { get; set; }

        /// <summary>
        /// Who created this user record (for audit). Usually same as RegisteredByUserId.
        /// </summary>
        public string? CreatedByUserId { get; set; }

        /// <summary>
        /// Who last modified this user record.
        /// </summary>
        public string? LastModifiedByUserId { get; set; }
    }
}
