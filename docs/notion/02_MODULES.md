# 📦 HRM Modules — Chi Tiết Nghiệp Vụ

---

## 🔐 Module 1: Authentication & Authorization

### UC-AUTH-01: Đăng nhập hệ thống

- **Actor**: Tất cả users
- **Precondition**: User đã được tạo tài khoản
- **Main Flow**:
    1. User nhập Username (hoặc Email) và Password
    2. Hệ thống xác thực thông tin
    3. Hệ thống tạo JWT Token (gồm UserId, EmployeeId, Roles)
    4. Trả về token + thông tin user
- **Business Rules**:
    - Hỗ trợ đăng nhập bằng Username HOẶC Email
    - Token JWT chứa claims: UserId, EmployeeId, Role
    - ✅ Check user bị khóa (Lockout) trước khi cho đăng nhập

### UC-AUTH-02: Đăng ký tài khoản (Admin Only)

- **Actor**: Admin
- **Main Flow**:
    1. Admin nhập: Username, Email, Password, FullName, EmployeeId (optional)
    2. Hệ thống validate: username unique
    3. Tạo user → gán role mặc định "Employee"
    4. Set EmailConfirmed = true
- **Business Rules**:
    - ✅ Check email trùng lặp khi đăng ký
    - Role mặc định: "Employee"

### UC-AUTH-03: Quản lý Role

- **Actor**: Admin
- **Operations**: Tạo role, Gán role cho user, Cập nhật roles, Xem danh sách roles
- **Roles hiện có**: Admin, HR, Manager, Employee

### UC-AUTH-04: Khóa/Mở khóa tài khoản

- **Actor**: Admin, HR
- **Main Flow**: Toggle trạng thái IsActive của user

### UC-AUTH-05: Đổi mật khẩu

- **Actor**: User đã đăng nhập
- **Main Flow**: Xác thực mật khẩu cũ → Đặt mật khẩu mới

---

## 🏢 Module 2: Organization Management

### 2A. Department (Phòng Ban)

**Data Model**: Name, Code, Description, ManagerId → Employee, ParentId → Department (self-referencing)

**Business Rules**:
- ✅ Hỗ trợ phân cấp đệ quy (phòng ban cha-con)
- ✅ Cycle Detection: Không cho A → B → A
- ✅ Cache tree bằng Redis (TTL 1 giờ), invalidate khi thay đổi
- ✅ Check nhân viên thuộc phòng ban trước khi xóa
- ✅ Hiển thị tên Manager thay vì ManagerId trong tree

### 2B. Position (Chức Vụ)

**Data Model**: Title, Code, SalaryRange (Min, Max, Currency=VND), DepartmentId → Department, ParentId → Position (self-referencing)

**Business Rules**:
- ✅ Phân cấp đệ quy (chức vụ cấp trên-dưới)
- ✅ Cycle Detection tương tự Department
- ✅ **Department Association**: Mỗi chức vụ luôn gắn liền với một phòng ban
- ✅ Check nhân viên đang giữ chức vụ trước khi xóa

---

## 👤 Module 3: Human Resource

### 3A. Employee (Nhân Viên)

#### UC-HR-01: Tạo hồ sơ nhân viên

- **Actor**: Admin, HR
- **Data Model**:
    - **PersonalInfo**: FullName, DateOfBirth, Gender, IdNumber, Phone, Email, Address, City
    - **JobDetails**: EmployeeCode, DepartmentId, PositionId, ManagerId, JoinDate, Status
    - **BankDetails**: BankName, AccountNumber, AccountHolder (🔒 restricted access)
    - AvatarUrl
- **Validation**:
    - ✅ Tuổi ≥ 18 (tính theo DateOfBirth)
    - ✅ EmployeeCode unique
    - ✅ DepartmentId, PositionId phải tồn tại
- **Side Effects (Event-Driven)**:
    1. Publish EmployeeCreatedEvent
    2. → CreateUserEventHandler: Tự động tạo tài khoản đăng nhập
        - Username = EmployeeCode
        - Email = Employee email
        - ✅ Password được sinh ngẫu nhiên hoặc theo policy an toàn
- **Transaction**: Unit of Work (MongoDB session)

#### UC-HR-02: Cập nhật hồ sơ

- Actor: Admin, HR
- Không được đổi EmployeeCode

#### UC-HR-03: Xóa nhân viên

- Actor: Admin Only
- ✅ KHÔNG cho xóa nếu đang là Manager phòng ban
- Publish EmployeeDeletedEvent
- ✅ Cleanup data liên quan thông qua EmployeeDeletedEventHandler

#### UC-HR-04: Xem hồ sơ

- 🔒 BankDetails chỉ hiển thị cho Admin, HR, hoặc chính nhân viên đó

### 3B. Contract (Hợp Đồng Lao Động)

#### UC-HR-06: Tạo hợp đồng

- **Actor**: Admin, HR
- **Data Model**:
    - EmployeeId, ContractCode, ContractType
    - StartDate, EndDate, SignDate
    - **SalaryComponents**: BaseSalary, Allowance, TransportAllowance, FoodAllowance, PhoneAllowance, OtherAllowance
    - Status: Active / Expired / Terminated
    - FileUrl, Note
- **Business Rules**:
    - ✅ Validate: StartDate < EndDate
    - ✅ Validate: BaseSalary > 0
    - ✅ Auto-Expire: Tạo HĐ mới → tất cả HĐ Active cũ → Expired
    - ✅ Overlap Check: Không cho HĐ Active trùng khoảng thời gian
    - ✅ Audit Log: Ghi lại old/new values khi thay đổi
- **Side Effect**: Publish ContractCreatedEvent → InitializeLeaveOnContractHandler

---

## ⏰ Module 4: Attendance Management

### 4A. Shift (Ca Làm Việc)

**Data Model**: Name, Code, StartTime, EndTime, BreakStartTime, BreakEndTime, GracePeriodMinutes (15 phút), IsOvernight, StandardWorkingHours, IsActive

### 4B. Check-In / Check-Out

- **Actor**: Tất cả users
- Ghi log CheckIn/CheckOut vào RawAttendanceLog

### 4C. Xử lý dữ liệu chấm công (Process Logs)

- **Actor**: Admin, HR (trigger thủ công)
- **Flow**:
    1. Lấy tất cả RawAttendanceLog chưa xử lý
    2. Group theo EmployeeId + Date
    3. Lấy Shift assignment cho mỗi nhân viên
    4. Tính toán qua AttendanceCalculator
    5. Lưu vào AttendanceBucket (1 document/employee/tháng)
    6. Đánh dấu Raw Log = Processed

### 4D. Logic tính trạng thái ngày công

| Điều kiện | Status | Chi tiết |
|-----------|--------|----------|
| Không có Shift | Present (No Shift) | Nếu có CheckIn |
| Không CheckIn | Absent | — |
| CheckIn > ShiftStart + GracePeriod | Late | LateMinutes = CheckIn - ShiftStart |
| CheckOut < ShiftEnd | Early Leave | EarlyLeaveMinutes = ShiftEnd - CheckOut |
| CheckOut > ShiftEnd + 15min | Tính OT | OT = (CheckOut - ShiftEnd) / 60 |
| Thứ 7, Chủ nhật | IsWeekend = true | — |

**Đặc biệt**:
- 🕐 Timezone: UTC → Local (+7:00 VN)
- 🍽️ Break Deduction: Tính overlap giữa work time và break time
- 🌙 Overnight shift: EndTime + 1 day nếu IsOvernight
- 👻 Ghost Log: CheckIn có, CheckOut không → xử lý graceful

---

## 🌴 Module 5: Leave Management

### 5A. LeaveType (Loại Phép)

| Thuộc tính | Mặc định | Mô tả |
|-----------|---------|-------|
| DefaultDaysPerYear | 12 | Số ngày/năm |
| IsAccrual | true | Cộng dồn theo tháng? |
| AccrualRatePerMonth | 1.0 | Tỉ lệ cộng/tháng |
| AllowCarryForward | false | Cho chuyển năm sau? |
| MaxCarryForwardDays | 0 | Max ngày chuyển |
| IsSandwichRuleApplied | false | Nghỉ T6+T2 → mất T7+CN |

### 5B. LeaveAllocation (Cấp Phát Phép)

- **Trigger**: ContractCreatedEvent → tạo allocation cho mỗi LeaveType active
- **Computed**: CurrentBalance = NumberOfDays + AccruedDays - UsedDays
- **Accrual**: Background Job chạy hàng tháng, idempotent (check LastAccrualMonth)

### 5C. LeaveRequest (Đơn Xin Phép)

**Lifecycle**:
- Employee tạo đơn → **Pending**
- Manager Review → **Approved** (trừ UsedDays) hoặc **Rejected**
- Employee có thể **Cancel** đơn Pending

**✅ ĐÃ HOÀN THIỆN:**
- ✅ Check CurrentBalance >= số ngày xin (trong CreateHandler)
- ✅ Check overlap với đơn khác (trong CreateHandler)
- ✅ Block sửa đơn đã Approved/Rejected (trong Domain Model)
- ✅ Ghi ApprovedBy khi Approve

---

## 💰 Module 6: Payroll

### Quy trình tính lương

1. Lấy thông tin NV + hợp đồng Active
2. Lấy dữ liệu chấm công tháng (AttendanceBucket)
3. Lấy ngày nghỉ phép đã duyệt
4. Lấy system settings (tỷ lệ BH, mức trần, giảm trừ)
5. Tính: Có công → Lương prorated → Phụ cấp → OT → Gross
6. Trừ: Bảo hiểm → Thuế TNCN → Nợ tháng trước → Net Salary
7. Lưu PayrollEntity + EmployeeSnapshot

### Biểu thuế TNCN lũy tiến (7 bậc)

| Bậc | Thu nhập chịu thuế (triệu VNĐ) | Thuế suất |
|-----|--------------------------------|-----------|
| 1 | Đến 5 | 5% |
| 2 | 5 → 10 | 10% |
| 3 | 10 → 18 | 15% |
| 4 | 18 → 32 | 20% |
| 5 | 32 → 52 | 25% |
| 6 | 52 → 80 | 30% |
| 7 | Trên 80 | 35% |

### Bảo hiểm

| Loại | Tỷ lệ NLĐ | Cơ sở tính |
|------|-----------|-----------|
| BHXH | 8% | Lương cơ bản (có trần) |
| BHYT | 1.5% | Lương cơ bản (có trần) |
| BHTN | 1% | Lương cơ bản (có trần) |

- ✅ **FIXED**: Đã chuyển sang tính trên baseSalary đúng quy định VN.

### Payroll Status: Draft → Confirmed → Paid (hoặc Rejected → Draft)

### EmployeeSnapshot

Chụp lại thông tin NV tại thời điểm tính lương (FullName, EmployeeCode, DepartmentId, PositionId, BaseSalary) → đảm bảo lịch sử chính xác.

---

## 📝 Module 7: Recruitment (HOÀN THIỆN)

**Đã có**: Entities (Candidate, Interview, JobVacancy) + Repositories

**Cần xây dựng**:
- Services, DTOs, Mappers, API Endpoints, Validators
- Workflow: Ứng tuyển → Sàng lọc → Phỏng vấn → Offer → Onboard

---

## ⚙️ Module 8: Common Services

- **Audit Log**: Ghi mọi thay đổi (UserId, Action, TableName, OldValues/NewValues JSON)
- **System Settings**: Key-Value store theo Group (Payroll, Attendance...)
- **Cache Service**: Redis — Set/Get/Remove với TTL
- **File Service**: Upload/Download files
- **Background Services**: LeaveAccrualService (cộng phép hàng tháng), AttendanceProcessingService
