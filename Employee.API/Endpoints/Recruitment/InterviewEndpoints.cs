using Carter;
using Employee.API.Common;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Commands.Interview.CreateInterview;
using Employee.Application.Features.Recruitment.Commands.Interview.UpdateInterview;
using Employee.Application.Features.Recruitment.Commands.Interview.DeleteInterview;
using Employee.Application.Features.Recruitment.Commands.Interview.ReviewInterview;
using Employee.Application.Features.Recruitment.Queries.Interview.GetInterviewsByCandidate;
using Employee.Application.Features.Recruitment.Queries.Interview.GetInterviewById;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Employee.API.Endpoints.Recruitment
{
  public class InterviewEndpoints : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/recruitment/interviews")
                     .WithTags("Recruitment - Interviews")
                     .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      group.MapGet("/", async ([FromQuery] string? candidateId, ISender sender) =>
      {
        var result = await sender.Send(new GetInterviewsByCandidateQuery(candidateId ?? string.Empty));
        return ResultUtils.Success(result, "Retrieved interviews successfully.");
      });

      group.MapGet("/{id}", async (string id, ISender sender) =>
      {
        var result = await sender.Send(new GetInterviewByIdQuery(id));
        return ResultUtils.Success(result);
      });

      group.MapPost("/", async ([FromBody] InterviewDto dto, ISender sender) =>
      {
        await sender.Send(new CreateInterviewCommand(dto));
        return ResultUtils.CreatedNoData("Interview scheduled successfully.");
      });

      group.MapPatch("/{id}", async (string id, [FromBody] InterviewDto dto, ISender sender) =>
      {
        await sender.Send(new UpdateInterviewCommand(id, dto));
        return ResultUtils.Success("Interview updated successfully.");
      });

      group.MapPost("/{id}/review", async (string id, [FromBody] ReviewInterviewDto dto, ISender sender) =>
      {
        await sender.Send(new ReviewInterviewCommand(id, dto));
        return ResultUtils.Success("Interview reviewed.");
      });

      group.MapDelete("/{id}", async (string id, ISender sender) =>
      {
        await sender.Send(new DeleteInterviewCommand(id));
        return ResultUtils.Success("Interview deleted.");
      });
    }
  }
}
