using Carter;
using Employee.API.Common;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Commands.Candidate.CreateCandidate;
using Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidate;
using Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidateStatus;
using Employee.Application.Features.Recruitment.Commands.Candidate.DeleteCandidate;
using Employee.Application.Features.Recruitment.Commands.OnboardCandidate;
using Employee.Application.Features.Recruitment.Queries.Candidate.GetCandidatesByVacancy;
using Employee.Application.Features.Recruitment.Queries.Candidate.GetCandidateById;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Employee.API.Endpoints.Recruitment
{
  public class CandidateEndpoints : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/recruitment/candidates")
                     .WithTags("Recruitment - Candidates")
                     .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      group.MapGet("/", async ([FromQuery] string? vacancyId, ISender sender) =>
      {
        var result = await sender.Send(new GetCandidatesByVacancyQuery(vacancyId ?? string.Empty));
        return ResultUtils.Success(result, "Retrieved candidates successfully.");
      });

      group.MapGet("/{id}", async (string id, ISender sender) =>
      {
        var result = await sender.Send(new GetCandidateByIdQuery(id));
        return result != null
            ? ResultUtils.Success(result)
            : ResultUtils.Fail("CANDIDATE_NOT_FOUND", "Candidate not found.");
      });

      group.MapPost("/", async ([FromBody] CandidateDto dto, ISender sender) =>
      {
        await sender.Send(new CreateCandidateCommand(dto));
        return ResultUtils.CreatedNoData("Candidate created successfully.");
      });

      group.MapPatch("/{id}", async (string id, [FromBody] CandidateDto dto, ISender sender) =>
      {
        await sender.Send(new UpdateCandidateCommand(id, dto));
        return ResultUtils.Success("Candidate updated successfully.");
      });

      group.MapPost("/{id}/status", async (string id, [FromBody] UpdateCandidateStatusDto dto, ISender sender) =>
      {
        await sender.Send(new UpdateCandidateStatusCommand(id, dto.Status));
        return ResultUtils.Success("Candidate status updated.");
      });

      group.MapPost("/{id}/onboard", async (string id, [FromBody] OnboardCandidateDto dto, ISender sender) =>
      {
        await sender.Send(new OnboardCandidateCommand { CandidateId = id, OnboardData = dto });
        return ResultUtils.Success("Candidate onboarded successfully.");
      });

      group.MapDelete("/{id}", async (string id, ISender sender) =>
      {
        await sender.Send(new DeleteCandidateCommand(id));
        return ResultUtils.Success("Candidate deleted.");
      });
    }
  }
}
