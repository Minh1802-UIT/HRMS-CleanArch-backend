using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.HumanResource.Dtos
{
  // ==========================================
  // A. NESTED DTOs (Các phần tử con)
  // ==========================================

  public class PersonalInfoDto
  {
    [Required(ErrorMessage = "Date of Birth is required.")]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    // Có thể validate: "Male", "Female", "Other"
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [MaxLength(15, ErrorMessage = "Phone number must not exceed 15 digits.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Identity Card (CCCD) is required.")]
    [MaxLength(20, ErrorMessage = "Identity Card must not exceed 20 characters.")]
    public string IdentityCard { get; set; } = string.Empty; // CCCD/CMND

    [MaxLength(500)]
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
    [Required(ErrorMessage = "Department ID is required.")]
    public string DepartmentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position ID is required.")]
    public string PositionId { get; set; } = string.Empty;

    public string? ManagerId { get; set; } // Thêm ManagerId
    public DateTime? ProbationEndDate { get; set; } // Thêm ngày kết thúc thử việc

    [Required(ErrorMessage = "Join Date is required.")]
    public DateTime JoinDate { get; set; }

    public string Status { get; set; } = "Active"; // Active, Resigned, OnLeave...

    // FE thường cần tên phòng ban/chức vụ để hiển thị
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }

    public string? ShiftName { get; set; }
    public string? ShiftId { get; set; }

    public string? ResumeUrl { get; set; }
    public string? ContractUrl { get; set; }
  }

  public class BankDetailsDto
  {
    [Required(ErrorMessage = "Bank Name is required.")]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account Number is required.")]
    [RegularExpression(@"^[0-9]+$", ErrorMessage = "Account Number can only contain digits.")]
    public string AccountNumber { get; set; } = string.Empty;

    public string? AccountHolder { get; set; } // Tên chủ tài khoản

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
    [Required(ErrorMessage = "Employee Code is required.")]
    [MaxLength(20, ErrorMessage = "Employee Code must not exceed 20 characters.")]
    [RegularExpression(@"^[A-Z0-9-_]+$", ErrorMessage = "Employee Code can only contain uppercase letters, numbers, hyphens, and underscores.")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [MaxLength(100, ErrorMessage = "Full Name must not exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    // Validate nested objects
    [Required(ErrorMessage = "Personal Info is required.")]
    public PersonalInfoDto PersonalInfo { get; set; } = new();

    [Required(ErrorMessage = "Job Details are required.")]
    public JobDetailsDto JobDetails { get; set; } = new();

    [Required(ErrorMessage = "Bank Details are required.")]
    public BankDetailsDto BankDetails { get; set; } = new();
  }

  // 3. UPDATE (Input)
  public class UpdateEmployeeDto
  {
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
    public int Version { get; set; }

    // Cho phép cập nhật từng phần
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