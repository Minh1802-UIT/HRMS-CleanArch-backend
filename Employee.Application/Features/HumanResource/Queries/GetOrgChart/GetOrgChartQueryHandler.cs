using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using MediatR;
using System.Collections.Generic;
using System.Linq;

namespace Employee.Application.Features.HumanResource.Queries.GetOrgChart
{
  public class GetOrgChartQueryHandler : IRequestHandler<GetOrgChartQuery, List<EmployeeOrgNodeDto>>
  {
    private readonly IEmployeeRepository _repo;
    private readonly IPositionRepository _posRepo;

    public GetOrgChartQueryHandler(IEmployeeRepository repo, IPositionRepository posRepo)
    {
      _repo = repo;
      _posRepo = posRepo;
    }

    public async Task<List<EmployeeOrgNodeDto>> Handle(GetOrgChartQuery request, CancellationToken cancellationToken)
    {
      var allEmployees = await _repo.GetAllAsync(cancellationToken);

      var activeEmployees = allEmployees
          .Where(e => e.JobDetails.Status == EmployeeStatus.Active || e.JobDetails.Status == EmployeeStatus.Probation)
          .ToList();

      var posIds = activeEmployees.Select(e => e.JobDetails.PositionId).Distinct().ToList();
      var positions = await _posRepo.GetNamesByIdsAsync(posIds, cancellationToken);

      var rootEmployees = activeEmployees.Where(e => string.IsNullOrEmpty(e.JobDetails.ManagerId)).ToList();

      var result = new List<EmployeeOrgNodeDto>();
      foreach (var root in rootEmployees)
      {
        result.Add(BuildEmployeeNode(root, activeEmployees, positions));
      }

      return result;
    }

    private EmployeeOrgNodeDto BuildEmployeeNode(EmployeeEntity emp, List<EmployeeEntity> allEmps, Dictionary<string, string> positions)
    {
      var node = new EmployeeOrgNodeDto
      {
        Id = emp.Id,
        Name = emp.FullName,
        Title = positions.GetValueOrDefault(emp.JobDetails.PositionId) ?? "N/A",
        AvatarUrl = emp.AvatarUrl,
        DepartmentId = emp.JobDetails.DepartmentId
      };

      var children = allEmps.Where(e => e.JobDetails.ManagerId == emp.Id).ToList();
      foreach (var child in children)
      {
        node.Children.Add(BuildEmployeeNode(child, allEmps, positions));
      }

      return node;
    }
  }
}
