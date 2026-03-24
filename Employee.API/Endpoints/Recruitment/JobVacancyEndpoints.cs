using Carter;
using Employee.API.Common;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Commands.JobVacancy.CreateJobVacancy;
using Employee.Application.Features.Recruitment.Commands.JobVacancy.UpdateJobVacancy;
using Employee.Application.Features.Recruitment.Commands.JobVacancy.DeleteJobVacancy;
using Employee.Application.Features.Recruitment.Commands.JobVacancy.CloseJobVacancy;
using Employee.Application.Features.Recruitment.Queries.GetAllJobVacancies;
using Employee.Application.Features.Recruitment.Queries.GetJobVacancyById;
using Employee.Application.Features.Recruitment.Queries.GetVacanciesPaged;
using Employee.Domain.Common.Models;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Employee.API.Endpoints.Recruitment
{
  public class JobVacancyEndpoints : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/recruitment/vacancies")
                     .WithTags("Recruitment - Job Vacancies")
                     .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // Static segments must be registered before /{id}, otherwise "options" is captured as an id (wrong handler, errors).
      // GET /api/recruitment/vacancies/options — static filter options for frontend dropdowns
      group.MapGet("/options", () =>
      {
        return ResultUtils.Success(new RecruitmentOptionsDto(), "Retrieved recruitment options.");
      });

      group.MapGet("/", async (
        [AsParameters] PaginationParams pagination,
        ISender sender) =>
      {
        if (pagination.PageSize.HasValue || !string.IsNullOrEmpty(pagination.SearchTerm))
        {
          var paged = await sender.Send(new GetVacanciesPagedQuery(pagination));
          return ResultUtils.Success(paged, "Retrieved paginated vacancies.");
        }
        var vacancies = await sender.Send(new GetAllJobVacanciesQuery());
        return ResultUtils.Success(vacancies, "Retrieved vacancies successfully.");
      });

      group.MapGet("/{id}", async (string id, ISender sender) =>
      {
        var result = await sender.Send(new GetJobVacancyByIdQuery(id));
        return ResultUtils.Success(result);
      });

      group.MapPost("/", async ([FromBody] JobVacancyDto dto, ISender sender) =>
      {
        await sender.Send(new CreateJobVacancyCommand(dto));
        return ResultUtils.CreatedNoData("Job vacancy created successfully.");
      });

      group.MapPatch("/{id}", async (string id, [FromBody] JobVacancyDto dto, ISender sender) =>
      {
        await sender.Send(new UpdateJobVacancyCommand(id, dto));
        return ResultUtils.Success("Job vacancy updated successfully.");
      });

      group.MapPost("/{id}/close", async (string id, ISender sender) =>
      {
        await sender.Send(new CloseJobVacancyCommand(id));
        return ResultUtils.Success("Job vacancy closed.");
      });

      group.MapDelete("/{id}", async (string id, ISender sender) =>
      {
        await sender.Send(new DeleteJobVacancyCommand(id));
        return ResultUtils.Success("Job vacancy deleted.");
      });
    }
  }
}
