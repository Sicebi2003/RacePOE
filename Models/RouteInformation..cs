using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api_proj.Models
{
    public class RouteInformation
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int RouteId { get; set; }

        public int EventId { get; set; }

        public string RouteName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public float DistanceKm { get; set; }
        //
        public float StartElevation { get; set; }

        public float EndElevation { get; set; }

        public string GPXData { get; set; } = string.Empty;
    }
}//