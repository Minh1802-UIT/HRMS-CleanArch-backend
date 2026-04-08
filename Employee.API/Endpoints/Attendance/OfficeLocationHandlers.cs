using Employee.API.Common;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Attendance
{
    public static class OfficeLocationHandlers
    {
        // GET /api/attendance/offices — all employees can see active offices
        public static async Task<IResult> GetActiveOffices(
            IOfficeLocationRepository repo)
        {
            var offices = await repo.GetAllActiveAsync();
            return ResultUtils.Success<object>(offices.Select(o => new
            {
                o.Id,
                o.Name,
                o.Address,
                o.Latitude,
                o.Longitude,
                o.RadiusMeters,
                o.IsRemote,
                o.IsActive
            }).ToList());
        }

        // POST /api/attendance/offices — Admin only
        public static async Task<IResult> CreateOffice(
            [FromBody] CreateOfficeDto dto,
            IOfficeLocationRepository repo)
        {
            var office = new OfficeLocation(
                dto.Name, dto.Latitude, dto.Longitude,
                dto.RadiusMeters, dto.Address, dto.IsRemote);
            await repo.CreateAsync(office);
            return ResultUtils.Success<object>(new { office.Id }, "Office created.");
        }

        // PUT /api/attendance/offices/{id} — Admin only
        public static async Task<IResult> UpdateOffice(
            string id,
            [FromBody] CreateOfficeDto dto,
            IOfficeLocationRepository repo)
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing == null) return ResultUtils.Fail("OFFICE_NOT_FOUND", "Office not found.");

            existing.Name = dto.Name;
            existing.Address = dto.Address;
            existing.Latitude = dto.Latitude;
            existing.Longitude = dto.Longitude;
            existing.RadiusMeters = dto.RadiusMeters;
            existing.IsRemote = dto.IsRemote;
            existing.IsActive = dto.IsActive;
            existing.SetUpdatedAt(DateTime.UtcNow);

            await repo.UpdateAsync(id, existing);
            return ResultUtils.Success("Office updated.");
        }

        // DELETE /api/attendance/offices/{id} — Admin only (soft delete)
        public static async Task<IResult> DeleteOffice(
            string id,
            IOfficeLocationRepository repo)
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing == null) return ResultUtils.Fail("OFFICE_NOT_FOUND", "Office not found.");

            existing.MarkDeleted(DateTime.UtcNow);
            await repo.UpdateAsync(id, existing);
            return ResultUtils.Success("Office deleted.");
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────
    public class CreateOfficeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusMeters { get; set; } = 500;
        public bool IsRemote { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
