using Carter;
using Employee.API.Common;
using Employee.Application.Features.Performance.Dtos;
using Employee.Application.Features.Performance.Commands.CreatePerformanceGoal;
using Employee.Application.Features.Performance.Commands.CreatePerformanceReview;
using Employee.Application.Features.Performance.Commands.UpdatePerformanceGoalProgress;
using Employee.Application.Features.Performance.Commands.UpdatePerformanceReview;
using Employee.Application.Features.Performance.Queries.GetEmployeeGoals;
using Employee.Application.Features.Performance.Queries.GetEmployeeReviews;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Employee.API.Endpoints.Performance
{
  public class PerformanceEndpoints : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/performance")
                     .WithTags("Performance Management")
                     .RequireAuthorization();

      // --- Goals ---
      group.MapGet("/goals/{employeeId}", async (string employeeId, ISender sender) =>
      {
        var result = await sender.Send(new GetEmployeeGoalsQuery(employeeId));
        return ResultUtils.Success(result, "Retrieved employee goals successfully.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      group.MapPost("/goals", async ([FromBody] PerformanceGoalDto dto, ISender sender) =>
      {
        var id = await sender.Send(new CreatePerformanceGoalCommand(dto));
        return ResultUtils.Created(id, "Performance goal created successfully.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      group.MapPut("/goals/{id}/progress", async (string id, [FromBody] double progress, ISender sender) =>
      {
        var success = await sender.Send(new UpdatePerformanceGoalProgressCommand(id, progress));
        return success ? ResultUtils.Success("Goal progress updated.") : ResultUtils.Fail("GOAL_NOT_FOUND", "Goal not found.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      // --- Reviews ---
      group.MapGet("/reviews/{employeeId}", async (string employeeId, ISender sender) =>
      {
        var result = await sender.Send(new GetEmployeeReviewsQuery(employeeId));
        return ResultUtils.Success(result, "Retrieved employee reviews successfully.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      group.MapPost("/reviews", async ([FromBody] PerformanceReviewDto dto, ISender sender) =>
      {
        var id = await sender.Send(new CreatePerformanceReviewCommand(dto));
        return ResultUtils.Created(id, "Performance review created successfully.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

      group.MapPut("/reviews/{id}", async (string id, [FromBody] PerformanceReviewDto dto, ISender sender) =>
      {
        var success = await sender.Send(new UpdatePerformanceReviewCommand(id, dto));
        return success ? ResultUtils.Success("Performance review updated.") : ResultUtils.Fail("REVIEW_NOT_FOUND", "Review not found.");
      }).RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));
    }
  }
}
