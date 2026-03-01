using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.Leave.Dtos;
using Employee.Application.Features.Leave.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;

namespace Employee.Application.Features.Leave.Queries.GetEmployeeLeaveRequests
{
  public class GetEmployeeLeaveRequestsQueryHandler : IRequestHandler<GetEmployeeLeaveRequestsQuery, IEnumerable<LeaveRequestDto>>
  {
    private readonly ILeaveRequestRepository _repo;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveTypeRepository _typeRepo;

    public GetEmployeeLeaveRequestsQueryHandler(
        ILeaveRequestRepository repo,
        IEmployeeRepository empRepo,
        ILeaveTypeRepository typeRepo)
    {
      _repo = repo;
      _empRepo = empRepo;
      _typeRepo = typeRepo;
    }

    public async Task<IEnumerable<LeaveRequestDto>> Handle(GetEmployeeLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
      var requests = await _repo.GetByEmployeeIdAsync(request.EmployeeId, cancellationToken);
      var emp = await _empRepo.GetByIdAsync(request.EmployeeId, cancellationToken);
      var name = emp?.FullName ?? "Unknown";
      var code = emp?.EmployeeCode ?? "Unknown";

      var types = await _typeRepo.GetPagedAsync(new PaginationParams { PageSize = 100 }, cancellationToken);
      var typesMap = types.Items.ToDictionary(k => k.Code, v => v.Name);

      return requests.Select(r =>
      {
        var typeCode = r.LeaveType.ToString();
        var typeName = typesMap.GetValueOrDefault(typeCode) ?? typeCode;
        return r.ToDto(name, code, leaveTypeName: typeName);
      }).ToList();
    }
  }
}
