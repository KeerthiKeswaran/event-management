using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Data.Contexts;
using Event.Models;

namespace Event.Data.Tests.Seed
{
    public class DbSeeder
    {
        private readonly EventDbContext _context;

        public DbSeeder(EventDbContext context)
        {
            _context = context;
        }

        public async Task<TermsAndConditions> SeedTermsAndConditionsAsync(string version = "v1.0", string type = "General")
        {
            var terms = new TermsAndConditions
            {
                Terms_Id = Guid.NewGuid().ToString(),
                Version = version,
                File_Path = $"/docs/policies/terms_{version}.md",
                Type = type,
                Is_Active = true,
                Created_At = DateTime.UtcNow
            };
            await _context.TermsAndConditions.AddAsync(terms);
            await _context.SaveChangesAsync();
            return terms;
        }

        public async Task<Region> SeedRegionAsync(string regionId = "US-EAST", int staffs = 5)
        {
            var region = new Region
            {
                Region_Id = regionId,
                No_Of_Staffs = staffs
            };
            await _context.Regions.AddAsync(region);
            await _context.SaveChangesAsync();
            return region;
        }

        public async Task<Venue> SeedVenueAsync(string regionId)
        {
            var venue = new Venue
            {
                Name = "Default Test Venue",
                Region_Id = regionId,
                Hourly_Price = 150.00m,
                Is_Available = true
            };
            await _context.Venues.AddAsync(venue);
            await _context.SaveChangesAsync();
            return venue;
        }

        public async Task<User> SeedUserAsync(string role = "Attendee", string? email = null)
        {
            var randomSuffix = new Random().Next(1000, 9999).ToString();
            var terms = await SeedTermsAndConditionsAsync($"v_{randomSuffix}");
            var user = new User
            {
                Name = $"Test {role}",
                Email = email ?? $"test_{role.ToLower()}_{randomSuffix}@example.com",
                Mobile_Number = "1234567890",
                Password_Hash = "hashed_password",
                Consented_Terms_Id = terms.Terms_Id,
                Has_Marketing_Consent = false
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<Event.Models.Event> SeedEventAsync(int organizerId, int? venueId = null)
        {
            var ev = new Event.Models.Event
            {
                Organizer_Id = organizerId,
                Venue_Id = venueId,
                Event_Type = venueId.HasValue ? "Physical" : "Virtual",
                Title = "Test Seed Event",
                Description_Url = "A seeded event for integration testing",
                Date_Time = DateTime.UtcNow.AddDays(7),
                Duration_Hours = 3,
                Status = "Live"
            };
            await _context.Events.AddAsync(ev);
            await _context.SaveChangesAsync();
            return ev;
        }
    }
}
