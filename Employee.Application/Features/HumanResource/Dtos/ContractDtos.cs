
namespace Employee.Application.Features.HumanResource.Dtos
{
    // ----------------------------------------------------
    // 1. SHARED DTOs (Dùng chung cho c? Create và Update)
    // ----------------------------------------------------

    public class SalaryInfoInputDto
    {
        public decimal BasicSalary { get; set; }
        public decimal TransportAllowance { get; set; }
        public decimal LunchAllowance { get; set; }
        public decimal OtherAllowance { get; set; }
    }

    // ----------------------------------------------------
    // 2. VIEW DTO (Output - Tr? v? cho Frontend)
    // ----------------------------------------------------
    public class ContractDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty; // FE c?n tên d? hi?n th?
        public string ContractCode { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty; // Fixed-Term, Indefinite...
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty; // Active, Expired, Terminated

        // Thông tin luong (Flatten ho?c Nested tùy convention, ? dây d? Nested cho g?n)
        public SalaryInfoDto Salary { get; set; } = new();
    }

    public class SalaryInfoDto
    {
        public decimal BasicSalary { get; set; }
        public decimal TotalSalary { get; set; } // Gross salary (Basic + Allowances)
                                                 // ... các ph? c?p khác
    }

    // ----------------------------------------------------
    // 3. CREATE DTO (Input)
    // ----------------------------------------------------
    public class CreateContractDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        // Mã h?p d?ng: Cho phép c? ch? hoa, ch? thu?ng, s?, d?u g?ch
        public string ContractCode { get; set; } = string.Empty;
        // Có th? validate c?ng các lo?i h?p d?ng ? dây n?u mu?n (VD: Probation, Official...)
        public string ContractType { get; set; } = "Fixed-Term";
        public DateTime StartDate { get; set; }

        // EndDate có th? null (H?p d?ng không xác d?nh th?i h?n)
        // Luu ý: Logic "EndDate > StartDate" nên d? Service check ho?c Custom Attribute
        public DateTime? EndDate { get; set; }
        public SalaryInfoInputDto Salary { get; set; } = new();
    }

    // ----------------------------------------------------
    // 4. UPDATE DTO (Input)
    // ----------------------------------------------------
    public class UpdateContractDto
    {
        public string Id { get; set; } = string.Empty;

        // Mã h?p d?ng (ContractCode) và EmployeeId thu?ng KHÔNG du?c s?a.
        // Ch? cho s?a ngày k?t thúc (Gia h?n) ho?c thông tin luong.

        public DateTime? EndDate { get; set; }

        // N?u c?p nh?t c? luong
        public SalaryInfoInputDto? Salary { get; set; }

        // Tr?ng thái (VD: Ch?m d?t h?p d?ng s?m)
        public string? Status { get; set; }
    }
}