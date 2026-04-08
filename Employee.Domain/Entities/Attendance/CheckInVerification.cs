using System;
using System.Collections.Generic;

namespace Employee.Domain.Entities.Attendance
{
    /// <summary>
    /// Value Object embedded on each RawAttendanceLog.
    /// Captures verification evidence and computes a Trust Score (0-100).
    /// </summary>
    public class CheckInVerification
    {
        // ── Trust Score ──────────────────────────────────────────────────────
        public int TrustScore { get; set; }
        /// <summary>"High" (80-100), "Medium" (40-79), "Low" (0-39)</summary>
        public string TrustLevel { get; set; } = "Low";

        // ── GPS Verification ─────────────────────────────────────────────────
        public bool GpsProvided { get; set; }
        public bool GpsWithinGeofence { get; set; }
        /// <summary>Distance in meters from the nearest office location.</summary>
        public double? DistanceToOfficeMeters { get; set; }
        public string? NearestOfficeId { get; set; }
        public string? NearestOfficeName { get; set; }
        /// <summary>The CheckInPoint ID that the employee selected on the frontend.</summary>
        public string? SelectedCheckInPointId { get; set; }

        // ── Photo Verification ───────────────────────────────────────────────
        public bool PhotoProvided { get; set; }

        // ── Device / Network Info (audit trail) ──────────────────────────────
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceFingerprint { get; set; }

        // ── Anomaly Detection Flags ──────────────────────────────────────────
        /// <summary>
        /// Human-readable warning codes, e.g.:
        /// "GPS_UNAVAILABLE", "GPS_OUTSIDE_GEOFENCE", "NO_PHOTO",
        /// "IMPOSSIBLE_TRAVEL", "DUPLICATE_DEVICE", "GPS_MISMATCH",
        /// "WFH_NOT_APPROVED"
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>True when TrustScore &lt; 40, triggers manager review queue.</summary>
        public bool RequiresReview { get; set; }

        /// <summary>Whether WFH was pre-approved for this day (only relevant for Remote check-in).</summary>
        public bool WfhApproved { get; set; }

        // Parameterless ctor for MongoDB deserialization
        public CheckInVerification() { }
    }
}
