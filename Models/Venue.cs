using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventEaseBookingSystem.Models
{
    public class Venue
    {
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Venue name is required.")]
        [StringLength(100, ErrorMessage = "Venue name cannot exceed 100 characters.")]
        public string VenueName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
        public string Location { get; set; } = string.Empty;

        [Range(1, 5000, ErrorMessage = "Capacity must be between 1 and 5000.")]
        public int Capacity { get; set; }

        [Range(100, 10000, ErrorMessage = "Hourly rate must be between 100 and 10000.")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [Required(ErrorMessage = "Image URL is required.")]
        [Url(ErrorMessage = "Please enter a valid image URL.")]
        public string ImageUrl { get; set; } = string.Empty;

        // Navigation property initialized to avoid null warnings
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
