using Employee.Domain.Entities.Common;
using System;

namespace Employee.Domain.Entities.Attendance
{
    /// <summary>
    /// Represents a physical office or check-in point.
    /// Admin manages these; employees select one when checking in.
    /// Backend uses coordinates + radius for geofence validation.
    /// </summary>
    public class OfficeLocation : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        /// <summary>Geofence radius in meters. Default 500m.</summary>
        public int RadiusMeters { get; set; } = 500;

        public bool IsActive { get; set; } = true;

        /// <summary>If true, employees can select this as a Remote/WFH point without GPS fence check.</summary>
        public bool IsRemote { get; set; } = false;

        // Parameterless ctor for MongoDB
        public OfficeLocation() { }

        public OfficeLocation(string name, double latitude, double longitude, int radiusMeters = 500, string? address = null, bool isRemote = false)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
            RadiusMeters = radiusMeters;
            Address = address;
            IsRemote = isRemote;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
