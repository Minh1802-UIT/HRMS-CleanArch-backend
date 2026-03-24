using Carter;
using Employee.API.Common;
using Employee.Application.Features.Performance.Commands.CreatePIP;
using Employee.Application.Features.Performance.Commands.UpdatePIPProgress;
using Employee.Application.Features.Performance.Commands.CompletePIP;
using Employee.Application.Features.Performance.Commands.StartPIP;
using Employee.Application.Features.Performance.Commands.CancelPIP;
using Employee.Application.Features.Performance.Queries.GetAllPIPs;
using Employee.Application.Features.Performance.Queries.GetPIPById;
using Employee.Application.Features.Performance.Queries.GetPerformanceAnalytics;
using Employee.Application.Features.Performance.Dtos;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Employee.API.Endpoints.Performance
{
  public class PIPEndpoints : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/performance/pip")
                     .WithTags("Performance - PIP")
                     .RequireAuthorization();

      // GET /api/performance/pip — list all active PIPs
      group.MapGet("/", async (ISender sender) =>
      {
        var result = await sender.Send(new GetAllPIPsQuery());
        return ResultUtils.Success(result, "Retrieved active PIPs successfully.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      // GET /api/performance/pip/{id}
      group.MapGet("/{id}", async (string id, ISender sender) =>
      {
        var result = await sender.Send(new GetPIPByIdQuery(id));
        return result == null
          ? ResultUtils.Fail("PIP_NOT_FOUND", "PIP not found.")
          : ResultUtils.Success(result);
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      // POST /api/performance/pip
      group.MapPost("/", async ([FromBody] PIPDto dto, ISender sender) =>
      {
        var id = await sender.Send(new CreatePIPCommand(dto));
        return ResultUtils.Created(id, "Performance Improvement Plan created successfully.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      // PATCH /api/performance/pip/{id}/start
      group.MapPatch("/{id}/start", async (string id, ISender sender) =>
      {
        var success = await sender.Send(new StartPIPCommand(id));
        return success
          ? ResultUtils.Success("PIP started.")
          : ResultUtils.Fail("PIP_NOT_FOUND", "PIP not found.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      // PATCH /api/performance/pip/{id}/progress
      group.MapPatch("/{id}/progress", async (string id, [FromBody] PIPUpdateProgressDto dto, ISender sender) =>
      {
        var success = await sender.Send(new UpdatePIPProgressCommand(id, dto));
        return success
          ? ResultUtils.Success("Objective progress updated.")
          : ResultUtils.Fail("PIP_NOT_FOUND", "PIP not found.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      // PATCH /api/performance/pip/{id}/complete
      group.MapPatch("/{id}/complete", async (string id, [FromBody] PIPCompleteDto dto, ISender sender) =>
      {
        var success = await sender.Send(new CompletePIPCommand(id, dto));
        return success
          ? ResultUtils.Success(dto.Successful ? "PIP completed successfully." : "PIP marked as failed.")
          : ResultUtils.Fail("PIP_NOT_FOUND", "PIP not found.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      // PATCH /api/performance/pip/{id}/cancel
      group.MapPatch("/{id}/cancel", async (string id, [FromQuery] string reason, ISender sender) =>
      {
        var success = await sender.Send(new CancelPIPCommand(id, reason));
        return success
          ? ResultUtils.Success("PIP cancelled.")
          : ResultUtils.Fail("PIP_NOT_FOUND", "PIP not found.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR"));
    }
  }

  public class PerformanceAnalyticsEndpoints : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/performance/analytics")
                     .WithTags("Performance - Analytics")
                     .RequireAuthorization();

      group.MapGet("/", async (ISender sender) =>
      {
        var result = await sender.Send(new GetPerformanceAnalyticsQuery());
        return ResultUtils.Success(result, "Retrieved performance analytics.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));
    }
  }
}
