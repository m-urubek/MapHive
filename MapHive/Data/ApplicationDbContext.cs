using MapHive.Models;
using Microsoft.EntityFrameworkCore;

namespace MapHive.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MapLocation> MapLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed some initial data
            modelBuilder.Entity<MapLocation>().HasData(
                new MapLocation
                {
                    Id = 1,
                    Name = "Sample Location 1",
                    Description = "This is a sample location",
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    Address = "New York, NY, USA",
                    Website = "https://example.com",
                    PhoneNumber = "123-456-7890"
                },
                new MapLocation
                {
                    Id = 2,
                    Name = "Sample Location 2",
                    Description = "This is another sample location",
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    Address = "Los Angeles, CA, USA",
                    Website = "https://example.org",
                    PhoneNumber = "987-654-3210"
                }
            );
        }
    }
} 