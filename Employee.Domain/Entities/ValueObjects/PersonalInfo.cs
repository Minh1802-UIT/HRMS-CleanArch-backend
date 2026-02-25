using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.ValueObjects
{
  public record PersonalInfo
  {
    public DateTime Dob { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string IdentityCard { get; init; } = string.Empty;
    public string MaritalStatus { get; init; } = string.Empty;
    public string Nationality { get; init; } = string.Empty;
    public string Hometown { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public int DependentCount { get; init; } = 0;
  }

  public record JobDetails
  {
    public string DepartmentId { get; init; } = string.Empty; // Reference
    public string PositionId { get; init; } = string.Empty;   // Reference
    public string ManagerId { get; init; } = string.Empty;    // Reference
    public string ShiftId { get; init; } = string.Empty;      // Reference to Shift
    public DateTime JoinDate { get; init; }
    public EmployeeStatus Status { get; init; } = EmployeeStatus.Probation;
    public string? ResumeUrl { get; init; }   // Resume URL
    public string? ContractUrl { get; init; } // Contract URL
    public DateTime? ProbationEndDate { get; init; } // End of probation period
  }

  public record BankDetails
  {
    public string BankName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountHolder { get; init; } = string.Empty;
    public string InsuranceCode { get; init; } = string.Empty;
    public string TaxCode { get; init; } = string.Empty;
  }
}