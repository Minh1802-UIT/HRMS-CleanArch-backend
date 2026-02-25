using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Organization.Dtos;
using Employee.Domain.Entities.Organization;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Queries.GetDepartmentTree
{
  public class GetDepartmentTreeQueryHandler(
      IDepartmentRepository repo,
      ICacheService cache,
      IEmployeeRepository employeeRepo) : IRequestHandler<GetDepartmentTreeQuery, List<DepartmentNodeDto>>
  {
    private const string DEPARTMENT_TREE_KEY = "DEPARTMENT_TREE";

    public async Task<List<DepartmentNodeDto>> Handle(GetDepartmentTreeQuery request, CancellationToken cancellationToken)
    {
      // 1. Check Cache
      var cachedTree = await cache.GetAsync<List<DepartmentNodeDto>>(DEPARTMENT_TREE_KEY);
      if (cachedTree != null) return cachedTree;

      // 2. If not in cache, get from DB
      var allDepts = await repo.GetAllActiveAsync(cancellationToken);
      var rootDepts = allDepts.Where(x => string.IsNullOrEmpty(x.ParentId)).ToList();

      // Fetch manager names for display
      var managerIds = allDepts.Select(d => d.ManagerId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
      var managerNames = await employeeRepo.GetNamesByIdsAsync(managerIds, cancellationToken);

      var result = new List<DepartmentNodeDto>();
      foreach (var root in rootDepts)
      {
        result.Add(BuildNode(root, allDepts, managerNames));
      }

      // 3. Save to Cache (1 hour)
      await cache.SetAsync(DEPARTMENT_TREE_KEY, result, TimeSpan.FromHours(1));

      return result;
    }

    private DepartmentNodeDto BuildNode(Department dept, List<Department> allDepts, Dictionary<string, (string Name, string Code)> managerNames)
    {
      var node = new DepartmentNodeDto
      {
        Id = dept.Id,
        Name = dept.Name,
        Code = dept.Code,
        ManagerName = !string.IsNullOrEmpty(dept.ManagerId) && managerNames.TryGetValue(dept.ManagerId, out var info) ? info.Name : null
      };

      var children = allDepts.Where(x => x.ParentId == dept.Id).ToList();
      foreach (var child in children)
      {
        node.Children.Add(BuildNode(child, allDepts, managerNames));
      }

      return node;
    }
  }
}
