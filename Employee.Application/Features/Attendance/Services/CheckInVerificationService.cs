using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Attendance.Services
{
    /// <summary>
    /// Computes a Trust Score (0-100) for each raw attendance log.
    /// Runs server-side after the log is created — never trusts client input blindly.
    /// 
    /// Score breakdown:
    ///   GPS inside geofence      +40
    ///   GPS available (outside)  +10
    ///   Selfie captured          +30
    ///   WFH approved (remote)    +20
    ///   Device consistency       +10  (future: hash comparison)
    ///   ---
    ///   Max                      100
    /// 
    /// Anomaly warnings:
    ///   GPS_UNAVAILABLE, GPS_OUTSIDE_GEOFENCE, GPS_MISMATCH,
    ///   NO_PHOTO, IMPOSSIBLE_TRAVEL, DUPLICATE_DEVICE,
    ///   WFH_NOT_APPROVED
    /// </summary>
    public class CheckInVerificationService
    {
        private readonly IOfficeLocationRepository _officeRepo;
        private readonly IWfhApprovalRepository _wfhRepo;
        private readonly IRawAttendanceLogRepository _rawLogRepo;
        private readonly ILogger<CheckInVerificationService> _logger;

        public CheckInVerificationService(
            IOfficeLocationRepository officeRepo,
            IWfhApprovalRepository wfhRepo,
            IRawAttendanceLogRepository rawLogRepo,
            ILogger<CheckInVerificationService> logger)
        {
            _officeRepo = officeRepo;
            _wfhRepo = wfhRepo;
            _rawLogRepo = rawLogRepo;
            _logger = logger;
        }

        public async Task<CheckInVerification> VerifyAsync(
            string employeeId,
            double? latitude,
            double? longitude,
            string? photoBase64,
            string? checkInPointId,
            string? userAgent,
            string? ipAddress,
            DateTime timestamp,
            CancellationToken ct = default)
        {
            var verification = new CheckInVerification
            {
                UserAgent = userAgent,
                IpAddress = ipAddress,
                SelectedCheckInPointId = checkInPointId
            };
            var warnings = new List<string>();
            int score = 0;

            // ── 1. Load offices ─────────────────────────────────────────────────
            var offices = await _officeRepo.GetAllActiveAsync(ct);
            var selectedOffice = offices.FirstOrDefault(o => o.Id == checkInPointId);
            var isRemotePoint = selectedOffice?.IsRemote == true;

            // ── 2. GPS Verification ─────────────────────────────────────────────
            if (latitude.HasValue && longitude.HasValue)
            {
                verification.GpsProvided = true;

                if (isRemotePoint)
                {
                    // Remote/WFH — GPS not meaningful for geofence, but still +10 for providing it
                    score += 10;
                }
                else if (offices.Any(o => !o.IsRemote))
                {
                    // Physical offices — find nearest and check geofence
                    var physicalOffices = offices.Where(o => !o.IsRemote).ToList();
                    var (nearest, distanceToNearest) = FindNearestOffice(latitude.Value, longitude.Value, physicalOffices);

                    // IMPORTANT FIX: Let's verify against the office they ACTUALLY selected (if it's physical) 
                    // instead of penalizing them against the nearest one which might have a smaller radius.
                    var targetOffice = (selectedOffice != null && !selectedOffice.IsRemote) ? selectedOffice : nearest;
                    var distance = targetOffice != null ? HaversineMeters(latitude.Value, longitude.Value, targetOffice.Latitude, targetOffice.Longitude) : distanceToNearest;

                    verification.DistanceToOfficeMeters = distance;
                    verification.NearestOfficeId = targetOffice?.Id;
                    verification.NearestOfficeName = targetOffice?.Name;

                    if (targetOffice != null && distance <= targetOffice.RadiusMeters)
                    {
                        verification.GpsWithinGeofence = true;
                        score += 60;
                    }
                    else
                    {
                        score += 10; // GPS available but outside fence
                        warnings.Add("GPS_OUTSIDE_GEOFENCE");
                        _logger.LogWarning(
                            "[Verification] Employee {EmpId} GPS outside geofence. Distance: {Dist:F0}m to {Office}",
                            employeeId, distance, targetOffice?.Name ?? "unknown");
                    }

                    // GPS mismatch: user selected office A but is closest to office B
                    if (selectedOffice != null && !selectedOffice.IsRemote && nearest != null
                        && selectedOffice.Id != nearest.Id)
                    {
                        warnings.Add("GPS_MISMATCH");
                    }
                }
            }
            else
            {
                warnings.Add("GPS_UNAVAILABLE");
            }

            // ── 3. Photo Verification ───────────────────────────────────────────
            if (!string.IsNullOrEmpty(photoBase64))
            {
                verification.PhotoProvided = true;
                score += 40;
            }
            else
            {
                warnings.Add("NO_PHOTO");
            }

            // ── 4. WFH Approval Check (Remote points only) ─────────────────────
            if (isRemotePoint)
            {
                var wfhApproval = await _wfhRepo.GetActiveApprovalAsync(employeeId, timestamp, ct);
                if (wfhApproval != null)
                {
                    verification.WfhApproved = true;
                    score += 50;
                }
                else
                {
                    warnings.Add("WFH_NOT_APPROVED");
                    _logger.LogWarning(
                        "[Verification] Employee {EmpId} checked in at Remote point without WFH approval.",
                        employeeId);
                }
            }

            // ── 5. Anomaly Detection: Impossible Travel ─────────────────────────
            if (latitude.HasValue && longitude.HasValue)
            {
                await CheckImpossibleTravel(employeeId, latitude.Value, longitude.Value, timestamp, warnings, ct);
            }

            // ── 6. Compute Trust Level ──────────────────────────────────────────
            verification.TrustScore = Math.Min(score, 100);
            verification.TrustLevel = score >= 80 ? "High" : score >= 40 ? "Medium" : "Low";
            verification.RequiresReview = score < 40;
            verification.Warnings = warnings;

            _logger.LogInformation(
                "[Verification] Employee {EmpId}: Score={Score} Level={Level} Warnings=[{Warnings}]",
                employeeId, verification.TrustScore, verification.TrustLevel,
                string.Join(", ", warnings));

            return verification;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private (OfficeLocation? nearest, double distanceMeters) FindNearestOffice(
            double lat, double lng, List<OfficeLocation> offices)
        {
            OfficeLocation? nearest = null;
            double minDistance = double.MaxValue;

            foreach (var office in offices)
            {
                var d = HaversineMeters(lat, lng, office.Latitude, office.Longitude);
                if (d < minDistance)
                {
                    minDistance = d;
                    nearest = office;
                }
            }

            return (nearest, minDistance);
        }

        /// <summary>
        /// Haversine formula — returns distance in METERS.
        /// </summary>
        public static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6_371_000; // Earth radius in meters
            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;

        /// <summary>
        /// Detect impossible travel: if the employee checked in from a location
        /// that is physically impossible to reach since their last check-in
        /// (e.g., 100km apart in 5 minutes = 1200 km/h, faster than a plane).
        /// Threshold: > 200 km/h average speed.
        /// </summary>
        private async Task CheckImpossibleTravel(
            string employeeId, double lat, double lng, DateTime timestamp,
            List<string> warnings, CancellationToken ct)
        {
            try
            {
                // Look back 24 hours for the most recent raw log with GPS
                var lookbackStart = timestamp.AddHours(-24);
                var recentLogs = await _rawLogRepo.GetByDateRangeAsync(employeeId, lookbackStart, timestamp, ct);
                var lastLog = recentLogs
                    .Where(l => l.Latitude.HasValue && l.Longitude.HasValue && l.Timestamp < timestamp)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefault();

                if (lastLog == null) return;

                var distance = HaversineMeters(lat, lng, lastLog.Latitude!.Value, lastLog.Longitude!.Value);
                var timeDiff = (timestamp - lastLog.Timestamp).TotalHours;

                if (timeDiff <= 0) return;

                var speedKmH = (distance / 1000.0) / timeDiff;

                // Flag if average speed > 200 km/h (impossible by car/public transport)
                if (speedKmH > 200)
                {
                    warnings.Add("IMPOSSIBLE_TRAVEL");
                    _logger.LogWarning(
                        "[Verification] IMPOSSIBLE_TRAVEL: Employee {EmpId} moved {Dist:F0}km in {Time:F1}h = {Speed:F0}km/h",
                        employeeId, distance / 1000, timeDiff, speedKmH);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Verification] Error checking impossible travel for {EmpId}", employeeId);
            }
        }
    }
}
