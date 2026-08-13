using EventEaseBookingSystem.Models; // lets me use Venue, Event, Booking classes
using Microsoft.EntityFrameworkCore; // gives access to DbContext, DbSet, etc.

namespace EventEaseBookingSystem.Data // keeps DbContext organized in Data namespace
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor: passes options (like connection string) to the base DbContext
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet represents a table in the database
        public DbSet<Venue> Venues { get; set; }     // Table for Venues
        public DbSet<Event> Events { get; set; }     // Table for Events
        public DbSet<Booking> Bookings { get; set; } // Table for Bookings

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Event → Bookings (cascade delete)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)   // explicitly link to Event.Bookings
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Venue → Bookings (restrict delete)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)   // explicitly link to Venue.Bookings
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Added seed data for Venues with correct PNG filenames
            modelBuilder.Entity<Venue>().HasData(
                new Venue { VenueId = 1, VenueName = "City Hall", Location = "Downtown Johannesburg", Capacity = 500, ImageUrl = "cityhall.png" },
                new Venue { VenueId = 2, VenueName = "Conference Center", Location = "Sandton Business District", Capacity = 1200, ImageUrl = "conferencecenter.png" },
                new Venue { VenueId = 3, VenueName = "Grand Hotel Ballroom", Location = "Pretoria Central", Capacity = 800, ImageUrl = "grandhotelballroom.png" },
                new Venue { VenueId = 4, VenueName = "Community Park", Location = "Cape Town Waterfront", Capacity = 350, ImageUrl = "communitypark.png" },
                new Venue { VenueId = 5, VenueName = "Sports Arena", Location = "Soweto, Johannesburg", Capacity = 5000, ImageUrl = "sportsarena.png" }
            );
        }
    }
}
