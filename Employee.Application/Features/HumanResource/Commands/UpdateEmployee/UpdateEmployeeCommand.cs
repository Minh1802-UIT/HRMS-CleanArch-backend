using Employee.Application.Features.HumanResource.Dtos;
using MediatR;

namespace Employee.Application.Features.HumanResource.Commands.UpdateEmployee
{
  public class UpdateEmployeeCommand : IRequest
  {
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Version { get; set; } // Optimistic Locking

    public PersonalInfoDto PersonalInfo { get; set; } = new();
    public JobDetailsDto JobDetails { get; set; } = new();
    public BankDetailsDto BankDetails { get; set; } = new();
  }
}
