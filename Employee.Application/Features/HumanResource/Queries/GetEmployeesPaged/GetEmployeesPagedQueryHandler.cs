using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using MediatR;
using System.Linq;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeesPaged
{
  public class GetEmployeesPagedQueryHandler : IRequestHandler<GetEmployeesPagedQuery, PagedResult<EmployeeListDto>>
  {
    private readonly IEmployeeRepository _repo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IPositionRepository _posRepo;

    public GetEmployeesPagedQueryHandler(
        IEmployeeRepository repo,
        IDepartmentRepository deptRepo,
        IPositionRepository posRepo)
    {
      _repo = repo;
      _deptRepo = deptRepo;
      _posRepo = posRepo;
    }

    public async Task<PagedResult<EmployeeListDto>> Handle(GetEmployeesPagedQuery request, CancellationToken cancellationToken)
    {
      var pagedEntities = await _repo.GetPagedAsync(request.Pagination, cancellationToken);

      var deptIds = pagedEntities.Items.Select(e => e.JobDetails.DepartmentId).Distinct().ToList();
      var posIds = pagedEntities.Items.Select(e => e.JobDetails.PositionId).Distinct().ToList();

      var depts = await _deptRepo.GetNamesByIdsAsync(deptIds, cancellationToken);
      var positions = await _posRepo.GetNamesByIdsAsync(posIds, cancellationToken);

      var dtos = pagedEntities.Items.Select(e => new EmployeeListDto
      {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FullName = e.FullName,
        DepartmentName = depts.GetValueOrDefault(e.JobDetails.DepartmentId) ?? "N/A",
        PositionName = positions.GetValueOrDefault(e.JobDetails.PositionId) ?? "N/A",
        Status = e.JobDetails.Status.ToString(),
        AvatarUrl = e.AvatarUrl
      }).ToList();

      return new PagedResult<EmployeeListDto>
      {
        Items = dtos,
        TotalCount = pagedEntities.TotalCount,
        PageNumber = pagedEntities.PageNumber,
        PageSize = pagedEntities.PageSize
      };
    }
  }
}
