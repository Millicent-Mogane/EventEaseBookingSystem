using EventEaseBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEaseBookingSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingSummaryView> BookingSummaryView { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BookingSummaryView>()
                .HasNoKey()
                .ToView("BookingSummaryView");

            modelBuilder.Entity<Venue>()
                .Property(v => v.HourlyRate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venue>().HasData(
                new Venue
                {
                    VenueId = 1,
                    VenueName = "City Hall",
                    Location = "Downtown Johannesburg",
                    Capacity = 500,
                    HourlyRate = 800,
                    ImageUrl = "/images/cityhall.png"
                },
                new Venue
                {
                    VenueId = 2,
                    VenueName = "Conference Center",
                    Location = "Sandton Business District",
                    Capacity = 1200,
                    HourlyRate = 1500,
                    ImageUrl = "/images/conferencecenter.png"
                },
                new Venue
                {
                    VenueId = 3,
                    VenueName = "Grand Hotel Ballroom",
                    Location = "Pretoria Central",
                    Capacity = 800,
                    HourlyRate = 1200,
                    ImageUrl = "/images/grandhotelballroom.png"
                },
                new Venue
                {
                    VenueId = 4,
                    VenueName = "Community Park",
                    Location = "Cape Town Waterfront",
                    Capacity = 350,
                    HourlyRate = 500,
                    ImageUrl = "/images/communitypark.png"
                },
                new Venue
                {
                    VenueId = 5,
                    VenueName = "Sports Arena",
                    Location = "Soweto, Johannesburg",
                    Capacity = 5000,
                    HourlyRate = 2500,
                    ImageUrl = "/images/sportsarena.png"
                }
            );
        }
    }
}
