
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Api_proj.Models
{
    public class Event
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Distance { get; set; }
        public string EventType { get; set; } = string.Empty;

        // Stores the event banner filename.
        public string? BannerImage { get; set; }

        public int? OrganiserId { get; set; }

        [ForeignKey("OrganiserId")]
        [JsonIgnore]
        public virtual User? Organiser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [JsonIgnore]
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

        [JsonIgnore]
        public virtual ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
