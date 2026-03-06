using Employee.Domain.Entities.Common;
using Employee.Domain.Entities.ValueObjects;

namespace Employee.Domain.Entities.HumanResource
{
    public class EmployeeEntity : BaseEntity
    {
    public string EmployeeCode { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }

    // 1. Embedded Personal Info
    public PersonalInfo PersonalInfo { get; private set; } = new();

    // 2. Embedded Job Details (Contains foreign keys)
    public JobDetails JobDetails { get; private set; } = new();

    // 3. Embedded Bank Details
    public BankDetails BankDetails { get; private set; } = new();

    // Constructor for EF Core
    private EmployeeEntity() { }

    // Factory Constructor
    public EmployeeEntity(string employeeCode, string fullName, string email)
    {
      if (string.IsNullOrWhiteSpace(employeeCode)) throw new ArgumentException("EmployeeCode is required.");
      if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.");
      if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");

      EmployeeCode = employeeCode;
      FullName = fullName;
      Email = email;
      CreatedAt = DateTime.UtcNow;
    }

    // Domain Methods
    public void UpdatePersonalInfo(PersonalInfo info)
    {
      if (info == null) throw new ArgumentNullException(nameof(info));

      // Domain Rule: Age >= 18
      var today = DateTime.Today;
      var age = today.Year - info.Dob.Year;
      if (info.Dob.Date > today.AddYears(-age)) age--;

      if (age < 18) throw new ArgumentException("Employee must be at least 18 years old.");

      PersonalInfo = info;
    }

    public void UpdateJobDetails(JobDetails job)
    {
      JobDetails = job ?? throw new ArgumentNullException(nameof(job));
    }

    public void UpdateBankDetails(BankDetails bank)
    {
      BankDetails = bank ?? throw new ArgumentNullException(nameof(bank));
    }

    public void UpdateAvatar(string? avatarUrl)
    {
      AvatarUrl = avatarUrl;
    }

    public void UpdateBasicInfo(string fullName, string email)
    {
      if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName cannot be empty.");
      if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email cannot be empty.");

      FullName = fullName;
      Email = email;
    }
    }
}