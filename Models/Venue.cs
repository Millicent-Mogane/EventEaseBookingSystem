namespace EventEaseBookingSystem.Models
{
    public class Venue
    {
        public int VenueId { get; set; }
        public required string VenueName { get; set; }   // must always have a value
        public required string Location { get; set; }
        public int Capacity { get; set; }
        public required string ImageUrl { get; set; }

        // Navigation property initialized to avoid null warnings
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

}
