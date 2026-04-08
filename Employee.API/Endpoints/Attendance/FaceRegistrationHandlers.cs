using Employee.API.Common;
using Employee.Application.Features.Attendance.Services;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Employee.API.Endpoints.Attendance
{
    public static class FaceRegistrationHandlers
    {
        // ── Employee self-register ──────────────────────────────────────
        // POST /api/attendance/face/register
        public static async Task<IResult> RegisterFace(
            [FromBody] RegisterFaceDto dto,
            IFaceEmbeddingRepository repo,
            ClaimsPrincipal user)
        {
            var employeeId = user.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
                return ResultUtils.Fail("FACE_FAILED","Cannot determine employee identity.");

            if (dto.Embedding == null || dto.Embedding.Length != 128)
                return ResultUtils.Fail("FACE_FAILED","Invalid embedding: must be 128-dimensional.");

            // Check if already has a pending or approved registration
            var existing = await repo.GetLatestByEmployeeAsync(employeeId);
            if (existing != null && existing.Status == FaceRegistrationStatus.Approved)
                return ResultUtils.Fail("FACE_FAILED","You already have an approved face registration. Contact HR to reset.");
            if (existing != null && existing.Status == FaceRegistrationStatus.Pending)
                return ResultUtils.Fail("FACE_FAILED","You already have a pending registration. Please wait for HR approval.");

            var face = new FaceEmbedding(employeeId, dto.Embedding, dto.PhotoThumbnail);
            await repo.CreateAsync(face);

            return ResultUtils.Success<object>(
                new { face.Id, face.Status },
                "Face registered. Awaiting HR approval.");
        }

        // ── Get own registration status ─────────────────────────────────
        // GET /api/attendance/face/my-status
        public static async Task<IResult> GetMyFaceStatus(
            IFaceEmbeddingRepository repo,
            ClaimsPrincipal user)
        {
            var employeeId = user.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
                return ResultUtils.Fail("FACE_FAILED","Cannot determine employee identity.");

            var face = await repo.GetLatestByEmployeeAsync(employeeId);
            if (face == null)
                return ResultUtils.Success<object>(new { registered = false });

            return ResultUtils.Success<object>(new
            {
                registered = true,
                face.Id,
                status = face.Status.ToString(),
                face.PhotoThumbnail,
                face.ReviewedBy,
                face.ReviewedAt,
                face.RejectionReason,
                face.CreatedAt
            });
        }

        // ── HR/Admin: Get all pending registrations ─────────────────────
        // GET /api/attendance/face/pending
        public static async Task<IResult> GetPendingRegistrations(
            IFaceEmbeddingRepository repo)
        {
            var pending = await repo.GetAllPendingAsync();
            return ResultUtils.Success<object>(pending.Select(f => new
            {
                f.Id,
                f.EmployeeId,
                f.PhotoThumbnail,
                status = f.Status.ToString(),
                f.CreatedAt
            }).ToList());
        }

        // ── HR/Admin: Get all registrations ─────────────────────────────
        // GET /api/attendance/face/all
        public static async Task<IResult> GetAllRegistrations(
            IFaceEmbeddingRepository repo)
        {
            var all = await repo.GetAllAsync();
            return ResultUtils.Success<object>(all.Select(f => new
            {
                f.Id,
                f.EmployeeId,
                f.PhotoThumbnail,
                status = f.Status.ToString(),
                f.ReviewedBy,
                f.ReviewedAt,
                f.RejectionReason,
                f.CreatedAt
            }).ToList());
        }

        // ── HR/Admin: Approve registration ──────────────────────────────
        // POST /api/attendance/face/{id}/approve
        public static async Task<IResult> ApproveRegistration(
            string id,
            IFaceEmbeddingRepository repo,
            ClaimsPrincipal user)
        {
            var reviewerId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var face = await repo.GetByIdAsync(id);
            if (face == null)
                return ResultUtils.Fail("FACE_FAILED","Registration not found.");

            face.Approve(reviewerId);
            await repo.UpdateAsync(face.Id, face);

            return ResultUtils.Success<object>(
                new { face.Id, status = face.Status.ToString() },
                "Face registration approved.");
        }

        // ── HR/Admin: Reject registration ───────────────────────────────
        // POST /api/attendance/face/{id}/reject
        public static async Task<IResult> RejectRegistration(
            string id,
            [FromBody] RejectFaceDto dto,
            IFaceEmbeddingRepository repo,
            ClaimsPrincipal user)
        {
            var reviewerId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var face = await repo.GetByIdAsync(id);
            if (face == null)
                return ResultUtils.Fail("FACE_FAILED","Registration not found.");

            face.Reject(reviewerId, dto.Reason);
            await repo.UpdateAsync(face.Id, face);

            return ResultUtils.Success<object>(
                new { face.Id, status = face.Status.ToString() },
                "Face registration rejected.");
        }

        // ── HR/Admin: Delete registration (allow re-register) ───────────
        // DELETE /api/attendance/face/{id}
        public static async Task<IResult> DeleteRegistration(
            string id,
            IFaceEmbeddingRepository repo)
        {
            await repo.DeleteAsync(id);
            return ResultUtils.Success<object>(null, "Face registration deleted.");
        }

        // ── Verify face during check-in ─────────────────────────────────
        // POST /api/attendance/face/verify
        public static async Task<IResult> VerifyFace(
            [FromBody] VerifyFaceDto dto,
            IFaceEmbeddingRepository repo,
            FaceVerificationService verificationService,
            ClaimsPrincipal user)
        {
            var employeeId = user.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
                return ResultUtils.Fail("FACE_FAILED","Cannot determine employee identity.");

            if (dto.Embedding == null || dto.Embedding.Length != 128)
                return ResultUtils.Fail("FACE_FAILED","Invalid embedding.");

            var registered = await repo.GetApprovedByEmployeeAsync(employeeId);
            if (registered == null)
                return ResultUtils.Success<object>(new
                {
                    matched = false,
                    reason = "NO_FACE_REGISTERED",
                    similarity = 0.0
                });

            var result = verificationService.Verify(dto.Embedding, registered.Embedding);
            return ResultUtils.Success<object>(new
            {
                matched = result.IsMatch,
                similarity = result.Similarity,
                threshold = result.Threshold
            });
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────

    public record RegisterFaceDto(float[] Embedding, string? PhotoThumbnail);
    public record VerifyFaceDto(float[] Embedding);
    public record RejectFaceDto(string? Reason);
}
