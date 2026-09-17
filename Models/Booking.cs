using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace EventEaseBookingSystem.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Booking date is required.")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        // Foreign Key to Event
        [Required(ErrorMessage = "Event selection is required.")]
        public int EventId { get; set; }

        [ValidateNever]
        public Event? Event { get; set; }

        // Foreign Key to Venue
        [Required(ErrorMessage = "Venue selection is required.")]
        public int VenueId { get; set; }

        [ValidateNever]
        public Venue? Venue { get; set; }

        // Booking status
        [Required(ErrorMessage = "Status is required.")]
        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string Status { get; set; } = "Confirmed";
    }
}
