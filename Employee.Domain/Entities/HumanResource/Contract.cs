using Employee.Domain.Entities.Common;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.HumanResource
{
    public class ContractEntity : BaseEntity
    {
    public string EmployeeId { get; private set; } = string.Empty; // FK
    public string ContractCode { get; private set; } = string.Empty;
    public string Type { get; private set; } = "Fixed-Term";
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SalaryComponents Salary { get; private set; } = new();
    public ContractStatus Status { get; private set; } = ContractStatus.Draft;
    public string? Note { get; private set; }
    public string? FileUrl { get; private set; }

    private ContractEntity() { }

    public ContractEntity(string employeeId, string contractCode, DateTime startDate)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(contractCode)) throw new ArgumentException("ContractCode is required.");

      EmployeeId = employeeId;
      ContractCode = contractCode;
      StartDate = startDate;
      Status = ContractStatus.Draft;
    }

    public void UpdateSalary(SalaryComponents salary)
    {
      Salary = salary ?? throw new ArgumentNullException(nameof(salary));
    }

    public void UpdateDates(DateTime startDate, DateTime? endDate)
    {
      if (startDate == default) throw new ArgumentException("StartDate is required.");
      if (endDate.HasValue && endDate.Value < startDate)
        throw new ArgumentException("EndDate cannot be before StartDate.");

      StartDate = startDate;
      EndDate = endDate;
    }

    public void Activate()
    {
      Status = ContractStatus.Active;
    }

    public void Terminate(string note)
    {
      Status = ContractStatus.Terminated;
      Note = note;
      EndDate = DateTime.UtcNow;
    }

    public void Expire(DateTime endDate)
    {
      Status = ContractStatus.Expired;
      EndDate = endDate;
    }

    public void UpdateFileUrl(string fileUrl)
    {
      FileUrl = fileUrl;
    }
    }
}