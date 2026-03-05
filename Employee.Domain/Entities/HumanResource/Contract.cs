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

    /// <summary>
    /// Schedules the contract for future activation when StartDate is in the future.
    /// Transitions: Draft → Pending.
    /// </summary>
    public void ScheduleActivation()
    {
      if (Status != ContractStatus.Draft)
        throw new InvalidOperationException($"Cannot schedule contract in '{Status}' status. Only Draft contracts can be scheduled.");
      Status = ContractStatus.Pending;
    }

    /// <summary>
    /// Activates the contract immediately.
    /// Transitions: Draft → Active OR Pending → Active (by background job on StartDate).
    /// </summary>
    public void Activate()
    {
      if (Status != ContractStatus.Draft && Status != ContractStatus.Pending)
        throw new InvalidOperationException($"Cannot activate contract in '{Status}' status. Only Draft or Pending contracts can be activated.");
      Status = ContractStatus.Active;
    }

    public void Terminate(string note, DateTime terminatedAt)
    {
      if (Status == ContractStatus.Terminated)
        throw new InvalidOperationException("Contract is already terminated.");
      if (Status == ContractStatus.Expired)
        throw new InvalidOperationException("Cannot terminate an already expired contract.");
      Status = ContractStatus.Terminated;
      Note = note;
      EndDate = terminatedAt;
    }

    public void Expire(DateTime endDate)
    {
      if (Status != ContractStatus.Active)
        throw new InvalidOperationException($"Cannot expire contract in '{Status}' status. Only Active contracts can expire.");
      Status = ContractStatus.Expired;
      EndDate = endDate;
    }

    public void UpdateFileUrl(string fileUrl)
    {
      FileUrl = fileUrl;
    }
    }
}
