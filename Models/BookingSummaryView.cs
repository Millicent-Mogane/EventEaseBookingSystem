using System;

namespace EventEaseBookingSystem.Models
{
    public class BookingSummaryView
    {
        public int BookingId { get; set; }
        public string EventName { get; set; }
        public string VenueName { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; }
    }
}
