using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Models;
using Employee.Application.Features.Leave.Dtos;
using MediatR;
using System.Linq;

namespace Employee.Application.Features.Leave.Queries.GetLeaveRequestsPaged
{
  public class GetLeaveRequestsPagedQueryHandler : IRequestHandler<GetLeaveRequestsPagedQuery, PagedResult<LeaveRequestListDto>>
  {
    private readonly ILeaveRequestRepository _repo;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveTypeRepository _typeRepo;

    public GetLeaveRequestsPagedQueryHandler(
        ILeaveRequestRepository repo,
        IEmployeeRepository empRepo,
        ILeaveTypeRepository typeRepo)
    {
      _repo = repo;
      _empRepo = empRepo;
      _typeRepo = typeRepo;
    }

    public async Task<PagedResult<LeaveRequestListDto>> Handle(GetLeaveRequestsPagedQuery request, CancellationToken cancellationToken)
    {
      var pagedRequests = await _repo.GetPagedAsync(request.Pagination, cancellationToken);

      var employeeIds = pagedRequests.Items.Select(r => r.EmployeeId).Distinct().ToList();
      var employeeNames = await _empRepo.GetNamesByIdsAsync(employeeIds, cancellationToken);

      var allLeaveTypes = await _typeRepo.GetPagedAsync(new PaginationParams { PageSize = 100 }, cancellationToken);
      var typeMap = allLeaveTypes.Items.ToDictionary(k => k.Code, v => v.Name);

      var dtos = pagedRequests.Items.Select(r =>
      {
        var typeCode = r.LeaveType.ToString();
        var empInfo = employeeNames.GetValueOrDefault(r.EmployeeId);
        return new LeaveRequestListDto
        {
          Id = r.Id,
          EmployeeCode = !string.IsNullOrEmpty(empInfo.Code) ? empInfo.Code : "Unknown",
          EmployeeName = !string.IsNullOrEmpty(empInfo.Name) ? empInfo.Name : "Unknown",
          AvatarUrl = null,
          LeaveType = typeMap.GetValueOrDefault(typeCode) ?? typeCode,
          FromDate = r.FromDate,
          ToDate = r.ToDate,
          Status = r.Status.ToString(),
          Reason = r.Reason
        };
      }).ToList();

      return new PagedResult<LeaveRequestListDto>
      {
        Items = dtos,
        TotalCount = pagedRequests.TotalCount,
        PageNumber = pagedRequests.PageNumber,
        PageSize = pagedRequests.PageSize
      };
    }
  }
}
