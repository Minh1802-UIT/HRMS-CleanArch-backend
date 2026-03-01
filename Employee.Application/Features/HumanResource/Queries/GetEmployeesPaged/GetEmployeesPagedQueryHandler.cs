using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using MediatR;
using System.Linq;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeesPaged
{
  public class GetEmployeesPagedQueryHandler : IRequestHandler<GetEmployeesPagedQuery, PagedResult<EmployeeListDto>>
  {
    private readonly IEmployeeQueryRepository _repo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IPositionRepository _posRepo;

    public GetEmployeesPagedQueryHandler(
        IEmployeeQueryRepository repo,
        IDepartmentRepository deptRepo,
        IPositionRepository posRepo)
    {
      _repo = repo;
      _deptRepo = deptRepo;
      _posRepo = posRepo;
    }

    public async Task<PagedResult<EmployeeListDto>> Handle(GetEmployeesPagedQuery request, CancellationToken cancellationToken)
    {
      // Use projection query — transfers only the fields the list page needs
      // (~500 bytes/employee instead of ~5 KB for a full EmployeeEntity).
      var pagedSummaries = await _repo.GetPagedListAsync(request.Pagination, cancellationToken);

      var deptIds = pagedSummaries.Items.Select(s => s.DepartmentId).Distinct().ToList();
      var posIds  = pagedSummaries.Items.Select(s => s.PositionId).Distinct().ToList();

      var depts     = await _deptRepo.GetNamesByIdsAsync(deptIds, cancellationToken);
      var positions = await _posRepo.GetNamesByIdsAsync(posIds, cancellationToken);

      var dtos = pagedSummaries.Items.Select(s => new EmployeeListDto
      {
        Id             = s.Id,
        EmployeeCode   = s.EmployeeCode,
        FullName       = s.FullName,
        DepartmentName = depts.GetValueOrDefault(s.DepartmentId)     ?? "N/A",
        PositionName   = positions.GetValueOrDefault(s.PositionId)   ?? "N/A",
        Status         = s.Status,
        AvatarUrl      = s.AvatarUrl
      }).ToList();

      return new PagedResult<EmployeeListDto>
      {
        Items      = dtos,
        TotalCount = pagedSummaries.TotalCount,
        PageNumber = pagedSummaries.PageNumber,
        PageSize   = pagedSummaries.PageSize
      };
    }
  }
}
