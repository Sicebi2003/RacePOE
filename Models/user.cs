
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Api_proj.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        [JsonIgnore]
        public string PasswordHash { get; set; }

        public int RoleId { get; set; }

        // Stores the profile image filename.
        public string? ProfileImage { get; set; }

        [ForeignKey("RoleId")]
        public Role Role { get; set; }

        [JsonIgnore]
        public ICollection<Enrolment> Enrolments { get; set; }

        [JsonIgnore]
        public ICollection<Event> OrganisedEvents { get; set; }
    }
}
