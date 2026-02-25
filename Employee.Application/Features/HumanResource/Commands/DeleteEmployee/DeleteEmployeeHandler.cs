using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.HumanResource.Events;
using MediatR;

namespace Employee.Application.Features.HumanResource.Commands.DeleteEmployee
{
  public class DeleteEmployeeHandler : IRequestHandler<DeleteEmployeeCommand>
  {
    private readonly IEmployeeRepository _repo;
    private readonly IPublisher _publisher;
    private readonly IDepartmentRepository _deptRepo;

    public DeleteEmployeeHandler(
        IEmployeeRepository repo,
        IPublisher publisher,
        IDepartmentRepository deptRepo)
    {
      _repo = repo;
      _publisher = publisher;
      _deptRepo = deptRepo;
    }

    public async Task Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
      var emp = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (emp == null) throw new NotFoundException($"Không tìm thấy nhân viên có ID '{request.Id}'");

      // 1. Kiểm tra: Không được xóa Manager đang quản lý phòng ban
      var isManager = await _deptRepo.ExistsByManagerIdAsync(request.Id, cancellationToken);
      if (isManager)
      {
        throw new ValidationException($"Không thể xóa nhân viên này vì đang là Quản lý (Manager) của một phòng ban. Vui lòng chuyển quyền quản lý trước.");
      }

      // 2. Xóa trong DB
      await _repo.DeleteAsync(request.Id, cancellationToken);

      // 2. 📢 Bắn sự kiện "Nhân viên đã bị xóa"
      // Event Handler sẽ lo việc: Xóa User Auth, Ghi Log, Xóa Hợp đồng...
      await _publisher.Publish(new EmployeeDeletedEvent(request.Id, emp.EmployeeCode, emp.FullName), cancellationToken);
    }
  }
}
