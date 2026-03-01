
namespace Employee.Application.Features.HumanResource.Dtos
{
  // ==========================================
  // A. NESTED DTOs (Các ph?n t? con)
  // ==========================================

  public class PersonalInfoDto
  {
    public DateTime DateOfBirth { get; set; }
    // Có th? validate: "Male", "Female", "Other"
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty; // CCCD/CMND
    public string Address { get; set; } = string.Empty;

    public string MaritalStatus { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Hometown { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
  }

  public class JobDetailsDto
  {
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;

    public string? ManagerId { get; set; } // Thêm ManagerId
    public DateTime? ProbationEndDate { get; set; } // Thêm ngày k?t thúc th? vi?c
    public DateTime JoinDate { get; set; }

    public string Status { get; set; } = "Active"; // Active, Resigned, OnLeave...

    // FE thu?ng c?n tên phòng ban/ch?c v? d? hi?n th?
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }

    public string? ShiftName { get; set; }
    public string? ShiftId { get; set; }

    public string? ResumeUrl { get; set; }
    public string? ContractUrl { get; set; }
  }

  public class BankDetailsDto
  {
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;

    public string? AccountHolder { get; set; } // Tên ch? tài kho?n

    public string InsuranceCode { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
  }

  // ==========================================
  // B. MAIN DTOs (Create / Update / View)
  // ==========================================

  // 1. VIEW (Output)
  public class EmployeeDto
  {
    public string Id { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Version { get; set; }

    public PersonalInfoDto PersonalInfo { get; set; } = new();
    public JobDetailsDto JobDetails { get; set; } = new();
    public BankDetailsDto? BankDetails { get; set; } = new();
  }

  // 2. CREATE (Input)
  public class CreateEmployeeDto
  {
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    // Validate nested objects
    public PersonalInfoDto PersonalInfo { get; set; } = new();
    public JobDetailsDto JobDetails { get; set; } = new();
    public BankDetailsDto BankDetails { get; set; } = new();
  }

  // 3. UPDATE (Input)
  public class UpdateEmployeeDto
  {
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
    public int Version { get; set; }

    // Cho phép c?p nh?t t?ng ph?n
    public PersonalInfoDto PersonalInfo { get; set; } = new();
    public JobDetailsDto JobDetails { get; set; } = new();
    public BankDetailsDto BankDetails { get; set; } = new();
  }

  public class EmployeeOrgNodeDto
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? DepartmentId { get; set; }
    public List<EmployeeOrgNodeDto> Children { get; set; } = new();
  }
}