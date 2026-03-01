using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Enums;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            var positions = await _posRepo.GetAllActiveAsync(cancellationToken);
            var posDict = positions.ToDictionary(p => p.Id, p => p);

            // Build Inferred Manager Tree
            var inferredManagers = new Dictionary<string, string>(); // EmployeeId -> ManagerId
            var childrenMap = new Dictionary<string, List<EmployeeEntity>>();

            foreach (var emp in activeEmployees)
            {
                string? managerId = emp.JobDetails.ManagerId;

                if (string.IsNullOrEmpty(managerId))
                {
                    // Auto-infer manager from Position hierarchy
                    var currentPosId = emp.JobDetails.PositionId;
                    while (true)
                    {
                        if (!posDict.TryGetValue(currentPosId, out var pos) || string.IsNullOrEmpty(pos.ParentId))
                            break; // Reached top level or position deleted

                        var manager = activeEmployees.FirstOrDefault(e => e.JobDetails.PositionId == pos.ParentId);
                        if (manager != null)
                        {
                            managerId = manager.Id;
                            break;
                        }

                        // Go up the chain
                        currentPosId = pos.ParentId;
                    }
                }

                if (!string.IsNullOrEmpty(managerId) && managerId != emp.Id)
                {
                    inferredManagers[emp.Id] = managerId;
                    if (!childrenMap.ContainsKey(managerId))
                        childrenMap[managerId] = new List<EmployeeEntity>();

                    childrenMap[managerId].Add(emp);
                }
            }

            var rootEmployees = activeEmployees.Where(e => !inferredManagers.ContainsKey(e.Id)).ToList();

            var result = new List<EmployeeOrgNodeDto>();
            foreach (var root in rootEmployees)
            {
                result.Add(BuildEmployeeNode(root, activeEmployees, posDict, childrenMap));
            }

            return result;
        }

        private EmployeeOrgNodeDto BuildEmployeeNode(EmployeeEntity emp, List<EmployeeEntity> allEmps, Dictionary<string, Position> posDict, Dictionary<string, List<EmployeeEntity>> childrenMap)
        {
            var node = new EmployeeOrgNodeDto
            {
                Id = emp.Id,
                Name = emp.FullName,
                Title = posDict.GetValueOrDefault(emp.JobDetails.PositionId)?.Title ?? "N/A",
                AvatarUrl = emp.AvatarUrl,
                DepartmentId = emp.JobDetails.DepartmentId
            };

            if (childrenMap.TryGetValue(emp.Id, out var children))
            {
                foreach (var child in children)
                {
                    node.Children.Add(BuildEmployeeNode(child, allEmps, posDict, childrenMap));
                }
            }

            return node;
        }
    }
}
