# MODULE SPECIFICATIONS — CHI TIẾT NGHIỆP VỤ

> Tài liệu chi tiết từng module, bao gồm Use Cases, Business Rules, và Workflows.

---

## MODULE 1: AUTHENTICATION & AUTHORIZATION

### 1.1 Use Cases

#### UC-AUTH-01: Đăng nhập hệ thống
- **Actor**: Tất cả users
- **Precondition**: User đã được tạo tài khoản
- **Trigger**: User truy cập trang Login
- **Main Flow**:
  1. User nhập Username (hoặc Email) và Password
  2. Hệ thống xác thực thông tin
  3. Hệ thống tạo JWT Token (gồm UserId, EmployeeId, Roles)
  4. Trả về token + thông tin user
- **Business Rules**:
  - Hỗ trợ đăng nhập bằng Username HOẶC Email
  - Token JWT chứa claims: `UserId`, `EmployeeId`, `Role`
  - ✅ **FIXED**: Check user bị khóa (Lockout) trước khi cho đăng nhập

#### UC-AUTH-02: Đăng ký tài khoản (Admin Only)
- **Actor**: Admin
- **Main Flow**:
  1. Admin nhập: Username, Email, Password, FullName, EmployeeId (optional)
  2. Hệ thống validate: username unique
  3. Tạo user → gán role mặc định "Employee"
  4. Set `EmailConfirmed = true`
- **Business Rules**:
  - ✅ **FIXED**: Check email trùng lặp khi đăng ký
  - Role mặc định: "Employee"

#### UC-AUTH-03: Quản lý Role
- **Actor**: Admin
- **Operations**: Tạo role, Gán role cho user, Cập nhật roles, Xem danh sách roles
- **Roles hiện có**: Admin, HR, Manager, Employee

#### UC-AUTH-04: Khóa/Mở khóa tài khoản
- **Actor**: Admin, HR
- **Main Flow**: Toggle trạng thái IsActive của user
- ✅ **FIXED**: Login handler đã check trạng thái Lockout/IsActive

#### UC-AUTH-05: Đổi mật khẩu
- **Actor**: User đã đăng nhập
- **Main Flow**: Xác thực mật khẩu cũ → Đặt mật khẩu mới

---

## MODULE 2: ORGANIZATION MANAGEMENT

### 2.1 Department (Phòng Ban)

#### UC-ORG-01: CRUD Phòng ban
- **Actor**: Admin, HR
- **Data Model**:
  - `Name`, `Code`, `Description`
  - `ManagerId` (FK → Employee)
  - `ParentId` (FK → Department, self-referencing)
- **Business Rules**:
  - Hỗ trợ **phân cấp đệ quy** (phòng ban cha-con)
  - **Cycle Detection**: Không cho phép A → B → A (detect vòng lặp khi cập nhật ParentId)
  - Cache tree bằng Redis (TTL 1 giờ), invalidate khi thay đổi
  - ✅ **FIXED**: Check nhân viên thuộc phòng ban trước khi xóa
  - ✅ **FIXED**: Hiển thị tên Manager thay vì ManagerId trong tree

#### UC-ORG-02: Xem sơ đồ tổ chức (Department Tree)
- **Actor**: Tất cả users
- **Output**: Cây phân cấp phòng ban (tree structure)
- **Caching**: Redis, 1 giờ

### 2.2 Position (Chức Vụ)

#### UC-ORG-03: CRUD Chức vụ
- **Actor**: Admin, HR
- **Data Model**:
  - `Title`, `Code`
  - `SalaryRange` { Min, Max, Currency = "VND" }
  - `DepartmentId` (FK → Department)
  - `ParentId` (FK → Position, self-referencing)
- **Business Rules**:
  - Phân cấp đệ quy (chức vụ cấp trên-dưới)
  - Cycle Detection tương tự Department
  - ✅ **Department Requirement**: Chức vụ phải thuộc về một Phòng ban cụ thể
  - ✅ **FIXED**: Check nhân viên đang giữ chức vụ trước khi xóa

---

## MODULE 3: HUMAN RESOURCE

### 3.1 Employee (Nhân Viên)

#### UC-HR-01: Tạo hồ sơ nhân viên
- **Actor**: Admin, HR
- **Data Model**:
  - **PersonalInfo**: FullName, DateOfBirth, Gender, IdNumber, Phone, Email, Address, City
  - **JobDetails**: EmployeeCode, DepartmentId, PositionId, ManagerId, JoinDate, Status
  - **BankDetails**: BankName, AccountNumber, AccountHolder
  - `AvatarUrl`
- **Validation**:
  - Tuổi ≥ 18 (tính theo DateOfBirth)
  - EmployeeCode unique
  - DepartmentId, PositionId phải tồn tại
- **Side Effects (Event-Driven)**:
  1. Publish `EmployeeCreatedEvent`
  2. → `CreateUserEventHandler`: Tự động tạo tài khoản đăng nhập
     - Username = EmployeeCode
     - Email = Employee email
     - ✅ **FIXED**: Password = `{employeeCode}@2025` (Auto-generated per employee)
  3. → Forward `ContractCreatedEvent` nếu có hợp đồng ban đầu
- **Transaction**: Unit of Work (MongoDB session)

#### UC-HR-02: Cập nhật hồ sơ nhân viên
- **Actor**: Admin, HR
- **Business Rules**: Không được đổi EmployeeCode

#### UC-HR-03: Xóa nhân viên
- **Actor**: Admin Only
- **Validation**:
  - ✅ KHÔNG cho xóa nếu đang là Manager của phòng ban (ExistsByManagerIdAsync)
- **Side Effects**: Publish `EmployeeDeletedEvent`
  - ⚠️ **[CẦN XÂY DỰNG]**: Handler cleanup LeaveAllocation, Attendance, Payroll, Contract

#### UC-HR-04: Xem hồ sơ nhân viên
- **Security**: BankDetails chỉ hiển thị cho Admin, HR, hoặc chính nhân viên đó

#### UC-HR-05: Xem sơ đồ nhân sự (Org Chart)
- Join Employee → Department → Position → Manager để xây dựng tree

### 3.2 Contract (Hợp Đồng Lao Động)

#### UC-HR-06: Tạo hợp đồng
- **Actor**: Admin, HR
- **Data Model**:
  - `EmployeeId`, `ContractCode`, `ContractType`
  - `StartDate`, `EndDate`, `SignDate`
  - **SalaryComponents**: { BaseSalary, Allowance, TransportAllowance, FoodAllowance, PhoneAllowance, OtherAllowance }
  - `Status`: Active, Expired, Terminated
  - `FileUrl`, `Note`
- **Business Rules**:
  - ✅ Validate: StartDate < EndDate
  - ✅ Validate: BaseSalary > 0
  - ✅ **Auto-Expire**: Khi tạo HĐ mới → tất cả HĐ Active cũ → Expired
  - ✅ **Overlap Check**: Không cho HĐ Active trùng khoảng thời gian
  - ✅ **Audit Log**: Ghi lại old/new values khi thay đổi
- **Side Effects**: Publish `ContractCreatedEvent`
  - → `InitializeLeaveOnContractHandler`: Khởi tạo leave allocation

#### UC-HR-07: Chấm dứt hợp đồng
- **Actor**: Admin, HR
- **Business Rules**: Set Status = Terminated, ghi Audit Log

---

## MODULE 4: ATTENDANCE MANAGEMENT

### 4.1 Shift (Ca Làm Việc)

#### UC-ATT-01: CRUD Ca làm
- **Data Model**:
  - `Name`, `Code`, `Description`
  - `StartTime`, `EndTime` (TimeSpan)
  - `BreakStartTime`, `BreakEndTime`
  - `GracePeriodMinutes` (mặc định 15 phút)
  - `IsOvernight` (ca qua đêm)
  - `StandardWorkingHours`
  - `IsActive`

### 4.2 Chấm Công

#### UC-ATT-02: Check-In / Check-Out
- **Actor**: Tất cả users
- **Main Flow**: Ghi log CheckIn/CheckOut vào RawAttendanceLog

#### UC-ATT-03: Xử lý dữ liệu chấm công (Process Logs)
- **Actor**: Admin, HR (trigger thủ công)
- **Main Flow**:
  1. Lấy tất cả RawAttendanceLog chưa xử lý
  2. Group theo EmployeeId + Date
  3. Lấy Shift assignment cho mỗi nhân viên
  4. Với mỗi ngày, tính toán qua `AttendanceCalculator`
  5. Lưu vào `AttendanceBucket` (Bucket Pattern — 1 document/employee/tháng)
  6. Đánh dấu Raw Log = Processed
- **Ghost Log**: Nếu có CheckIn mà không CheckOut → tự động set CheckOut = null, trạng thái "Present (No Shift)" hoặc tính theo giờ CheckIn

#### UC-ATT-04: Tính trạng thái ngày công
- **Calculator Logic** (pure function):
  - Input: DailyLog + Shift
  - Output: Status, WorkingHours, LateMinutes, EarlyLeaveMinutes, OvertimeHours

| Điều kiện | Status | Ghi chú |
|-----------|--------|---------|
| Không có Shift | "Present (No Shift)" | Nếu có CheckIn |
| Không CheckIn | "Absent" | |
| CheckIn > ShiftStart + GracePeriod | "Late" | LateMinutes = CheckIn - ShiftStart |
| CheckOut < ShiftEnd | "Early Leave" | EarlyLeaveMinutes = ShiftEnd - CheckOut |
| CheckOut > ShiftEnd + 15min | Tính OT | OvertimeHours = (CheckOut - ShiftEnd) / 60 |
| Thứ 7, Chủ nhật | IsWeekend = true | |

- **Timezone**: UTC → Local (+7:00)
- **Break Deduction**: Tính overlap giữa [CheckIn, CheckOut] và [BreakStart, BreakEnd]
- **WorkingHours** = Duration - BreakOverlap (min 0)

### 4.3 AttendanceBucket (Bucket Pattern)
- 1 document = 1 nhân viên × 1 tháng
- Contains: List<DailyLog>, TotalPresent, TotalLateMinutes, TotalOvertimeHours
- Auto-recalculate totals khi update

---

## MODULE 5: LEAVE MANAGEMENT

### 5.1 LeaveType (Loại Phép)

#### UC-LV-01: CRUD Loại phép
- **Data Model**:
  - `Name` (Annual Leave, Sick Leave...)
  - `Code` (AL, SL...)
  - `DefaultDaysPerYear` (mặc định 12)
  - `IsAccrual` (cộng dồn theo tháng?)
  - `AccrualRatePerMonth` (tỉ lệ cộng/tháng, mặc định 1.0)
  - `AllowCarryForward` (cho chuyển sang năm sau?)
  - `MaxCarryForwardDays`
  - `IsSandwichRuleApplied` (nghỉ T6+T2 → mất luôn T7+CN)
  - `IsActive`

### 5.2 LeaveAllocation (Cấp Phát Phép)

#### UC-LV-02: Khởi tạo phép năm
- **Trigger**: `ContractCreatedEvent` → `InitializeLeaveOnContractHandler`
- **Logic**: Tạo LeaveAllocation cho mỗi LeaveType active, năm hiện tại
- **Fields**: NumberOfDays, AccruedDays, UsedDays
- **Computed**: `CurrentBalance = NumberOfDays + AccruedDays - UsedDays`

#### UC-LV-03: Cộng dồn phép hàng tháng (Accrual)
- **Trigger**: Background Job (`LeaveAccrualBackgroundService` — chạy 1h/lần, check mỗi ngày 1 tháng)
- **Logic**:
  1. Lấy tất cả employees Active
  2. Với mỗi employee + LeaveType IsAccrual
  3. Check `LastAccrualMonth` tránh cộng trùng (idempotent)
  4. AccruedDays += AccrualRatePerMonth
  5. Update LastAccrualMonth
- ⚠️ **[CẦN SỬA]**: Filter employee `Status == "Active"` nhưng entity dùng "Probation"/"Official"

### 5.3 LeaveRequest (Đơn Xin Phép)

#### UC-LV-04: Tạo đơn xin phép
- **Actor**: Tất cả users
- **Flow**: Employee chọn LeaveType, FromDate, ToDate, Reason → Submit
- **Status ban đầu**: "Pending"
- ✅ **FIXED**:
  - Check `CurrentBalance >= NumberOfDays` (trong CreateHandler)
  - Check overlap với đơn khác (trong CreateHandler)

#### UC-LV-05: Duyệt đơn (Approve/Reject)
- **Actor**: Admin, HR, Manager
- **Approve Flow**:
  1. Set Status = "Approved"
  2. Tính số ngày nghỉ (business logic)
  3. Cập nhật `UsedDays` trong LeaveAllocation
  4. Sử dụng Unit of Work (transaction)
- **Reject Flow**: Set Status = "Rejected", ghi ManagerComments

#### UC-LV-06: Hủy đơn
- **Actor**: Owner (employee đã tạo đơn)
- **Precondition**: Status = "Pending"
- ⚠️ **[CẦN XEM XÉT]**: Cho hủy đơn đã Approved? (cần RefundDays)

#### UC-LV-07: Sửa đơn
- **Actor**: Owner
- ✅ **FIXED**: Chỉ cho sửa khi Status = "Pending" (Domain check)

---

## MODULE 6: PAYROLL

### 6.1 Quy Trình Tính Lương

#### UC-PAY-01: Generate Payroll
- **Actor**: Admin, HR
- **Input**: Month, Year, EmployeeId
- **Main Flow**:

```
1. Lấy thông tin nhân viên + hợp đồng Active
2. Lấy dữ liệu chấm công tháng (AttendanceBucket)
3. Lấy ngày nghỉ phép đã duyệt (LeaveRequest Approved)
4. Lấy system settings (tỷ lệ BH, mức trần, giảm trừ)
5. TÍNH LƯƠNG:
   a. Có công thực tế = PresentDays + LeaveDays
   b. Lương thực nhận = (BaseSalary / StandardDays) × ActualDays
   c. Phụ cấp = Tổng allowances (theo hợp đồng)
   d. Gross = Prorated + Allowances + OT Pay
   e. Bảo hiểm (BHXH + BHYT + BHTN)
   f. Thu nhập chịu thuế = Gross - BH - Giảm trừ cá nhân - Giảm trừ người phụ thuộc
   g. Thuế TNCN (biểu lũy tiến 7 bậc)
   h. Nợ tháng trước (carry-forward)
   i. Net Salary = Gross - BH - Tax - Debt
6. Lưu PayrollEntity + EmployeeSnapshot
```

### 6.2 Biểu Thuế TNCN Lũy Tiến (7 Bậc)

| Bậc | Thu nhập chịu thuế (triệu VNĐ) | Thuế suất |
|-----|--------------------------------|-----------|
| 1 | Đến 5 | 5% |
| 2 | 5 → 10 | 10% |
| 3 | 10 → 18 | 15% |
| 4 | 18 → 32 | 20% |
| 5 | 32 → 52 | 25% |
| 6 | 52 → 80 | 30% |
| 7 | Trên 80 | 35% |

### 6.3 Bảo Hiểm (theo quy định VN)

| Loại | Tỷ lệ NLĐ | Ghi chú |
|------|-----------|---------|
| BHXH | 8% | Bảo hiểm xã hội |
| BHYT | 1.5% | Bảo hiểm y tế |
| BHTN | 1% | Bảo hiểm thất nghiệp |

> [!IMPORTANT]
> **STATUS**: Bảo hiểm đã được fix tính trên `baseSalary` (lương cơ bản) trong `PayrollProcessingService.cs`.

### 6.4 EmployeeSnapshot
- Chụp lại thông tin nhân viên **tại thời điểm tính lương**
- Include: FullName, EmployeeCode, DepartmentId, PositionId, BaseSalary
- Mục đích: Đảm bảo dữ liệu lịch sử không bị ảnh hưởng bởi thay đổi sau này

### 6.5 Payroll Status Workflow

```
Draft → Confirmed → Paid
         ↓
       Rejected (quay lại Draft để tính lại)
```

#### UC-PAY-02: Xem phiếu lương cá nhân
- **Actor**: Tất cả users (chỉ xem của mình)

#### UC-PAY-03: Xem bảng lương công ty
- **Actor**: Admin, HR

#### UC-PAY-04: Duyệt/Xác nhận lương
- **Actor**: Admin, HR

---

## MODULE 7: RECRUITMENT (CHƯA HOÀN THIỆN)

### 7.1 Entities đã có
- `Candidate` (ứng viên)
- `Interview` (lịch phỏng vấn)
- `JobVacancy` (tin tuyển dụng)
- Repositories đã đăng ký

### 7.2 Cần xây dựng
- [ ] Services (CandidateService, InterviewService, JobVacancyService)
- [ ] DTOs + Mappers
- [ ] API Endpoints
- [ ] Validators
- [ ] Workflow: Ứng tuyển → Sàng lọc → Phỏng vấn → Offer → Onboard

---

## MODULE 8: COMMON SERVICES

### 8.1 Audit Log
- Ghi lại mọi thay đổi: UserId, UserName, Action, TableName, RecordId, OldValues, NewValues
- OldValues/NewValues lưu dạng JSON string

### 8.2 System Settings
- Key-Value store, phân theo Group (Payroll, Attendance...)
- Dùng cho: tỷ lệ BH, mức trần BH, giảm trừ cá nhân, số ngày công chuẩn...

### 8.3 Cache Service (Redis)
- Set/Get/Remove với TTL
- Dùng cho: Department Tree, Position Tree

### 8.4 File Service
- Upload/Download files (contract attachments, avatars)

### 8.5 Background Services
- `LeaveAccrualBackgroundService`: Cộng dồn phép hàng tháng (chạy periodic)
- `AttendanceProcessingBackgroundService`: Xử lý log chấm công tự động
