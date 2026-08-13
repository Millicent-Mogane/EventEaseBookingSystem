using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EventEaseBookingSystem.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        public DateTime BookingDate { get; set; }

        // Foreign Key to Event
        public int EventId { get; set; }

        [ValidateNever]
        public Event? Event { get; set; }

        // Foreign Key to Venue
        public int VenueId { get; set; }

        [ValidateNever]
        public Venue? Venue { get; set; }
    }
}