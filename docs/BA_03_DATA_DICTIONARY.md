# DATA DICTIONARY — TỪ ĐIỂN DỮ LIỆU

> Mô tả chi tiết tất cả Entities, Value Objects, và quan hệ trong hệ thống.

---

## 1. BASE ENTITY (Abstract)

Tất cả entities kế thừa từ `BaseEntity`:

| Field | Type | Mô tả |
|-------|------|--------|
| `Id` | string | MongoDB ObjectId (auto-generated) |
| `IsDeleted` | bool | Soft delete flag (default: false) |
| `CreatedAt` | DateTime | Ngày tạo (UTC) |
| `UpdatedAt` | DateTime? | Ngày cập nhật cuối (UTC) |
| `Version` | int | Optimistic concurrency version |

---

## 2. AUTH ENTITIES

### 2.1 ApplicationUser (Collection: `users`)
Kế thừa `MongoIdentityUser<Guid>` → có sẵn: UserName, Email, PasswordHash, Roles...

| Field | Type | Mô tả |
|-------|------|--------|
| `FullName` | string | Tên đầy đủ |
| `EmployeeId` | string? | FK → Employee (1:1) |

### 2.2 ApplicationRole (Collection: `roles`)
Kế thừa `MongoIdentityRole<Guid>`

| Values | Admin, HR, Manager, Employee |
|--------|------------------------------|

---

## 3. ORGANIZATION ENTITIES

### 3.1 Department (Collection: `departments`)

| Field | Type | Mô tả | Constraint |
|-------|------|--------|-----------|
| `Name` | string | Tên phòng ban | Required |
| `Code` | string | Mã phòng ban | Required |
| `Description` | string | Mô tả | |
| `ManagerId` | string? | FK → Employee | |
| `ParentId` | string? | FK → Department (self) | Cycle detection |

### 3.2 Position (Collection: `positions`)

| Field | Type | Mô tả | Constraint |
|-------|------|--------|-----------|
| `Title` | string | Tên chức vụ | Required |
| `Code` | string | Mã chức vụ | Required |
| `SalaryRange` | SalaryRange | Khung lương | Embedded VO |
| `DepartmentId` | string | FK → Department | Required |
| `ParentId` | string? | FK → Position (self) | Cycle detection |

**Value Object — SalaryRange**:

| Field | Type | Default | Mô tả |
|-------|------|---------|--------|
| `Min` | decimal | 0 | Lương tối thiểu |
| `Max` | decimal | 0 | Lương tối đa |
| `Currency` | string | "VND" | Đơn vị tiền tệ |

---

## 4. HUMAN RESOURCE ENTITIES

### 4.1 EmployeeEntity (Collection: `employees`)

| Field | Type | Mô tả |
|-------|------|--------|
| `EmployeeCode` | string | Mã NV (unique) |
| `FullName` | string | Tên đầy đủ |
| `AvatarUrl` | string? | Link ảnh đại diện |
| `PersonalInfo` | PersonalInfo | Thông tin cá nhân (embedded) |
| `JobDetails` | JobDetails | Thông tin công việc (embedded) |
| `BankDetails` | BankDetails | Thông tin ngân hàng (embedded, restricted) |

**Value Object — PersonalInfo**:

| Field | Type | Mô tả |
|-------|------|--------|
| `DateOfBirth` | DateTime? | Ngày sinh (validate ≥ 18 tuổi) |
| `Gender` | string | Giới tính |
| `IdNumber` | string | CMND/CCCD |
| `Phone` | string | Số điện thoại |
| `Email` | string | Email cá nhân |
| `Address` | string | Địa chỉ |
| `City` | string | Thành phố |

**Value Object — JobDetails**:

| Field | Type | Mô tả |
|-------|------|--------|
| `DepartmentId` | string | FK → Department |
| `PositionId` | string | FK → Position |
| `ManagerId` | string? | FK → Employee (quản lý trực tiếp) |
| `JoinDate` | DateTime | Ngày vào làm |
| `Status` | string | Trạng thái: Probation, Official, Resigned |

**Value Object — BankDetails** (🔒 Restricted):

| Field | Type | Mô tả |
|-------|------|--------|
| `BankName` | string | Tên ngân hàng |
| `AccountNumber` | string | Số tài khoản |
| `AccountHolder` | string | Chủ tài khoản |

> **Security**: Chỉ Admin, HR, hoặc chính nhân viên mới xem được BankDetails.

### 4.2 ContractEntity (Collection: `contracts`)

| Field | Type | Mô tả |
|-------|------|--------|
| `EmployeeId` | string | FK → Employee |
| `ContractCode` | string | Mã hợp đồng |
| `ContractType` | string | Loại HĐ: Probation, Fixed-Term, Indefinite |
| `StartDate` | DateTime | Ngày bắt đầu |
| `EndDate` | DateTime? | Ngày kết thúc |
| `SignDate` | DateTime? | Ngày ký |
| `SalaryComponents` | SalaryComponents | Cấu trúc lương (embedded) |
| `Status` | string | Active, Expired, Terminated |
| `FileUrl` | string? | File scan hợp đồng |
| `Note` | string? | Ghi chú |

**Value Object — SalaryComponents**:

| Field | Type | Mô tả |
|-------|------|--------|
| `BaseSalary` | decimal | Lương cơ bản |
| `Allowance` | decimal | Phụ cấp chung |
| `TransportAllowance` | decimal | Phụ cấp đi lại |
| `FoodAllowance` | decimal | Phụ cấp ăn trưa |
| `PhoneAllowance` | decimal | Phụ cấp điện thoại |
| `OtherAllowance` | decimal | Phụ cấp khác |

---

## 5. ATTENDANCE ENTITIES

### 5.1 Shift (Collection: `shifts`)

| Field | Type | Default | Mô tả |
|-------|------|---------|--------|
| `Name` | string | | Tên ca: Ca Sáng, Hành chính... |
| `Code` | string | | Mã ca: S01, S02... |
| `StartTime` | TimeSpan | | Giờ bắt đầu |
| `EndTime` | TimeSpan | | Giờ kết thúc |
| `BreakStartTime` | TimeSpan | | Giờ nghỉ bắt đầu |
| `BreakEndTime` | TimeSpan | | Giờ nghỉ kết thúc |
| `GracePeriodMinutes` | int | 15 | Phút ân hạn |
| `IsOvernight` | bool | false | Ca qua đêm |
| `StandardWorkingHours` | double | | Giờ công chuẩn/ca |
| `IsActive` | bool | true | Trạng thái |

### 5.2 AttendanceBucket (Collection: `attendance_buckets`)
**Pattern**: Bucket Pattern — 1 document per employee per month

| Field | Type | Mô tả |
|-------|------|--------|
| `EmployeeId` | string | FK → Employee |
| `Month` | string | Format: "MM-yyyy" |
| `DailyLogs` | List\<DailyLog\> | Danh sách log ngày (embedded) |
| `TotalPresentDays` | int | Tổng ngày đi làm |
| `TotalLateDays` | int | Tổng ngày đi trễ |
| `TotalLateMinutes` | int | Tổng phút đi trễ |
| `TotalOvertimeHours` | double | Tổng giờ OT |

**Value Object — DailyLog** (embedded in AttendanceBucket):

| Field | Type | Mô tả |
|-------|------|--------|
| `Date` | DateTime | Ngày |
| `CheckIn` | DateTime? | Giờ vào (UTC) |
| `CheckOut` | DateTime? | Giờ ra (UTC) |
| `Status` | string | Present, Late, Early Leave, Absent |
| `WorkingHours` | double | Giờ làm thực tế |
| `LateMinutes` | int | Phút đi trễ |
| `EarlyLeaveMinutes` | int | Phút về sớm |
| `OvertimeHours` | double | Giờ tăng ca |
| `IsWeekend` | bool | Ngày cuối tuần |
| `IsHoliday` | bool | Ngày lễ |
| `ShiftCode` | string? | Mã ca làm |
| `Note` | string? | Ghi chú |

---

## 6. LEAVE ENTITIES

### 6.1 LeaveType (Collection: `leave_types`)

| Field | Type | Default | Mô tả |
|-------|------|---------|--------|
| `Name` | string | | Annual Leave, Sick Leave... |
| `Code` | string | | AL, SL... |
| `Description` | string | | Mô tả |
| `DefaultDaysPerYear` | int | 12 | Số ngày/năm |
| `IsAccrual` | bool | true | Cộng dồn theo tháng? |
| `AccrualRatePerMonth` | double | 1.0 | Tỉ lệ cộng/tháng |
| `AllowCarryForward` | bool | false | Cho chuyển năm sau? |
| `MaxCarryForwardDays` | int | 0 | Max ngày chuyển |
| `IsSandwichRuleApplied` | bool | false | Áp dụng Sandwich Rule? |
| `DefaultDays` | int | | (Redundant with DefaultDaysPerYear) |
| `IsActive` | bool | true | |

### 6.2 LeaveAllocation (Collection: `leave_allocations`)

| Field | Type | Mô tả |
|-------|------|--------|
| `EmployeeId` | string | FK → Employee |
| `LeaveTypeId` | string | FK → LeaveType |
| `Year` | string | Năm: "2026" |
| `NumberOfDays` | double | Số ngày cấp đầu kỳ |
| `AccruedDays` | double | Số ngày cộng dồn |
| `UsedDays` | double | Số ngày đã dùng |
| `LastAccrualMonth` | string? | Tháng cộng cuối: "2026-02" (idempotent) |
| `CurrentBalance` | double | **Computed**: NumberOfDays + AccruedDays - UsedDays |

### 6.3 LeaveRequest (Collection: `leave_requests`)

| Field | Type | Mô tả |
|-------|------|--------|
| `EmployeeId` | string | FK → Employee |
| `LeaveType` | string | Tên loại phép |
| `FromDate` | DateTime | Ngày bắt đầu nghỉ |
| `ToDate` | DateTime | Ngày kết thúc nghỉ |
| `Reason` | string | Lý do |
| `Status` | string | Pending → Approved / Rejected |
| `ManagerComments` | string? | Ghi chú từ manager |

---

## 7. PAYROLL ENTITIES

### 7.1 PayrollEntity (Collection: `payrolls`)

| Field | Type | Mô tả |
|-------|------|--------|
| `EmployeeId` | string | FK → Employee |
| `Month` | int | Tháng tính lương |
| `Year` | int | Năm |
| `EmployeeSnapshot` | EmployeeSnapshot | Chụp thông tin NV (embedded) |
| **Attendance Data** | | |
| `StandardWorkingDays` | int | Ngày công chuẩn |
| `ActualWorkingDays` | double | Ngày công thực tế |
| `PaidLeaveDays` | double | Ngày phép có lương |
| `UnpaidLeaveDays` | double | Ngày nghỉ không lương |
| `OvertimeHours` | double | Giờ OT |
| `LateCount` | int | Số lần đi trễ |
| **Salary Calculation** | | |
| `BaseSalary` | decimal | Lương cơ bản (từ HĐ) |
| `TotalAllowances` | decimal | Tổng phụ cấp |
| `OvertimePay` | decimal | Tiền OT |
| `Bonus` | decimal | Thưởng |
| `GrossIncome` | decimal | Tổng thu nhập |
| `EmployeeBHXH` | decimal | BHXH (8%) |
| `EmployeeBHYT` | decimal | BHYT (1.5%) |
| `EmployeeBHTN` | decimal | BHTN (1%) |
| `TaxableIncome` | decimal | Thu nhập chịu thuế |
| `PersonalIncomeTax` | decimal | Thuế TNCN |
| `TotalDeductions` | decimal | Tổng khấu trừ |
| `PreviousDebt` | decimal | Nợ tháng trước |
| `FinalNetSalary` | decimal | Lương thực nhận |
| `Status` | string | Draft, Confirmed, Paid, Rejected |
| `Note` | string? | Ghi chú |

**Value Object — EmployeeSnapshot** (immutable):

| Field | Type | Mô tả |
|-------|------|--------|
| `FullName` | string | Tên NV tại thời điểm tính |
| `EmployeeCode` | string | Mã NV |
| `DepartmentId` | string | Phòng ban |
| `PositionId` | string | Chức vụ |
| `BaseSalary` | decimal | Lương cơ bản |

---

## 8. COMMON ENTITIES

### 8.1 AuditLog (Collection: `audit_logs`)

| Field | Type | Mô tả |
|-------|------|--------|
| `UserId` | string | Người thao tác |
| `UserName` | string | Tên người thao tác |
| `Action` | string | Create, Update, Delete |
| `TableName` | string | Bảng bị tác động |
| `RecordId` | string | ID bản ghi |
| `OldValues` | string? | JSON string |
| `NewValues` | string? | JSON string |

### 8.2 SystemSetting (Collection: `system_settings`)

| Field | Type | Mô tả |
|-------|------|--------|
| `Key` | string | Tên setting |
| `Value` | string | Giá trị |
| `Description` | string | Mô tả |
| `Group` | string | Nhóm: Payroll, Attendance... |

---

## 9. ENTITY RELATIONSHIP DIAGRAM

```
Employee ─1:1── User (Auth)
    │
    ├─1:N── Contract ──triggers──▶ ContractCreatedEvent
    │                                    │
    │                                    ▼
    │                          LeaveAllocation (Initialize)
    │
    ├─1:N── LeaveRequest ──approved──▶ LeaveAllocation (Deduct)
    │
    ├─1:N── AttendanceBucket (1 per month, contains DailyLogs)
    │
    ├─1:N── Payroll (1 per month)
    │
    ├─N:1── Department ──self-ref──▶ Department (parent)
    │           │
    │           └──N:1── Employee (Manager)
    │
    └─N:1── Position ──┬─self-ref──▶ Position (parent)
                       └─N:1───────▶ Department

LeaveType ─1:N── LeaveAllocation
Shift ─1:N── DailyLog (via ShiftCode)
```
