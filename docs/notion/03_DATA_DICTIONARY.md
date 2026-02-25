# 🗄️ Data Dictionary — Từ Điển Dữ Liệu

---

## 🔧 Base Entity (tất cả entities kế thừa)

| Field | Type | Mô tả |
|-------|------|--------|
| Id | string | MongoDB ObjectId (auto) |
| IsDeleted | bool | Soft delete (default: false) |
| CreatedAt | DateTime | Ngày tạo (UTC) |
| UpdatedAt | DateTime? | Ngày cập nhật cuối |
| Version | int | Optimistic concurrency |

---

## 🔐 Auth Entities

### ApplicationUser (Collection: users)

Kế thừa MongoIdentityUser → có sẵn: UserName, Email, PasswordHash, Roles...

| Field | Type | Mô tả |
|-------|------|--------|
| FullName | string | Tên đầy đủ |
| EmployeeId | string? | FK → Employee (1:1) |

### ApplicationRole (Collection: roles)

Values: Admin, HR, Manager, Employee

---

## 🏢 Organization Entities

### Department (Collection: departments)

| Field | Type | Mô tả |
|-------|------|--------|
| Name | string | Tên phòng ban |
| Code | string | Mã phòng ban |
| Description | string | Mô tả |
| ManagerId | string? | FK → Employee |
| ParentId | string? | FK → Department (self, cycle detection) |

### Position (Collection: positions)

| Field | Type | Mô tả |
|-------|------|--------|
| Title | string | Tên chức vụ |
| Code | string | Mã chức vụ |
| SalaryRange.Min | decimal | Lương tối thiểu |
| SalaryRange.Max | decimal | Lương tối đa |
| SalaryRange.Currency | string | "VND" |
| DepartmentId | string | FK → Department |
| ParentId | string? | FK → Position (self, cycle detection) |

---

## 👤 Employee Entity (Collection: employees)

| Field | Type | Mô tả |
|-------|------|--------|
| EmployeeCode | string | Mã NV (unique) |
| FullName | string | Tên đầy đủ |
| AvatarUrl | string? | Link ảnh đại diện |

### Embedded: PersonalInfo

| Field | Type | Mô tả |
|-------|------|--------|
| DateOfBirth | DateTime? | Ngày sinh (validate ≥ 18 tuổi) |
| Gender | string | Giới tính |
| IdNumber | string | CMND/CCCD |
| Phone | string | Số điện thoại |
| Email | string | Email cá nhân |
| Address | string | Địa chỉ |
| City | string | Thành phố |

### Embedded: JobDetails

| Field | Type | Mô tả |
|-------|------|--------|
| DepartmentId | string | FK → Department |
| PositionId | string | FK → Position |
| ManagerId | string? | FK → Employee (quản lý trực tiếp) |
| JoinDate | DateTime | Ngày vào làm |
| Status | string | Probation / Official / Resigned |

### Embedded: BankDetails 🔒

| Field | Type | Mô tả |
|-------|------|--------|
| BankName | string | Tên ngân hàng |
| AccountNumber | string | Số tài khoản |
| AccountHolder | string | Chủ tài khoản |

🔒 Chỉ Admin, HR, hoặc chính NV mới xem được.

---

## 📄 Contract Entity (Collection: contracts)

| Field | Type | Mô tả |
|-------|------|--------|
| EmployeeId | string | FK → Employee |
| ContractCode | string | Mã hợp đồng |
| ContractType | string | Probation / Fixed-Term / Indefinite |
| StartDate | DateTime | Ngày bắt đầu |
| EndDate | DateTime? | Ngày kết thúc |
| SignDate | DateTime? | Ngày ký |
| Status | string | Active / Expired / Terminated |
| FileUrl | string? | File scan hợp đồng |
| Note | string? | Ghi chú |

### Embedded: SalaryComponents

| Field | Type | Mô tả |
|-------|------|--------|
| BaseSalary | decimal | Lương cơ bản |
| Allowance | decimal | Phụ cấp chung |
| TransportAllowance | decimal | Phụ cấp đi lại |
| FoodAllowance | decimal | Phụ cấp ăn trưa |
| PhoneAllowance | decimal | Phụ cấp điện thoại |
| OtherAllowance | decimal | Phụ cấp khác |

---

## ⏰ Shift Entity (Collection: shifts)

| Field | Type | Default | Mô tả |
|-------|------|---------|--------|
| Name | string | — | Tên ca |
| Code | string | — | Mã ca |
| StartTime | TimeSpan | — | Giờ bắt đầu |
| EndTime | TimeSpan | — | Giờ kết thúc |
| BreakStartTime | TimeSpan | — | Giờ nghỉ bắt đầu |
| BreakEndTime | TimeSpan | — | Giờ nghỉ kết thúc |
| GracePeriodMinutes | int | 15 | Phút ân hạn |
| IsOvernight | bool | false | Ca qua đêm |
| StandardWorkingHours | double | — | Giờ công chuẩn/ca |

---

## ⏰ AttendanceBucket (Collection: attendance_buckets)

1 document = 1 nhân viên × 1 tháng (Bucket Pattern)

| Field | Type | Mô tả |
|-------|------|--------|
| EmployeeId | string | FK → Employee |
| Month | string | Format: "MM-yyyy" |
| DailyLogs | List | Danh sách log ngày |
| TotalPresentDays | int | Tổng ngày đi làm |
| TotalLateDays | int | Tổng ngày đi trễ |
| TotalLateMinutes | int | Tổng phút đi trễ |
| TotalOvertimeHours | double | Tổng giờ OT |

### Embedded: DailyLog

| Field | Type | Mô tả |
|-------|------|--------|
| Date | DateTime | Ngày |
| CheckIn | DateTime? | Giờ vào (UTC) |
| CheckOut | DateTime? | Giờ ra (UTC) |
| Status | string | Present / Late / Early Leave / Absent |
| WorkingHours | double | Giờ làm thực tế |
| LateMinutes | int | Phút đi trễ |
| EarlyLeaveMinutes | int | Phút về sớm |
| OvertimeHours | double | Giờ tăng ca |
| IsWeekend | bool | Ngày cuối tuần |
| ShiftCode | string? | Mã ca làm |

---

## 🌴 Leave Entities

### LeaveType (Collection: leave_types)

| Field | Type | Default | Mô tả |
|-------|------|---------|--------|
| Name | string | — | Annual Leave, Sick Leave... |
| Code | string | — | AL, SL... |
| DefaultDaysPerYear | int | 12 | Số ngày/năm |
| IsAccrual | bool | true | Cộng dồn theo tháng? |
| AccrualRatePerMonth | double | 1.0 | Tỉ lệ cộng/tháng |
| AllowCarryForward | bool | false | Cho chuyển năm sau? |
| MaxCarryForwardDays | int | 0 | Max ngày chuyển |
| IsSandwichRuleApplied | bool | false | Sandwich Rule? |

### LeaveAllocation (Collection: leave_allocations)

| Field | Type | Mô tả |
|-------|------|--------|
| EmployeeId | string | FK → Employee |
| LeaveTypeId | string | FK → LeaveType |
| Year | string | "2026" |
| NumberOfDays | double | Số ngày cấp đầu kỳ |
| AccruedDays | double | Số ngày cộng dồn |
| UsedDays | double | Số ngày đã dùng |
| LastAccrualMonth | string? | "2026-02" (idempotent) |
| **CurrentBalance** | **computed** | **= NumberOfDays + AccruedDays - UsedDays** |

### LeaveRequest (Collection: leave_requests)

| Field | Type | Mô tả |
|-------|------|--------|
| EmployeeId | string | FK → Employee |
| LeaveType | string | Tên loại phép |
| FromDate | DateTime | Ngày bắt đầu nghỉ |
| ToDate | DateTime | Ngày kết thúc nghỉ |
| Reason | string | Lý do |
| Status | string | Pending / Approved / Rejected |
| ManagerComments | string? | Ghi chú từ manager |

---

## 💰 Payroll Entity (Collection: payrolls)

### Thông tin chung

| Field | Type | Mô tả |
|-------|------|--------|
| EmployeeId | string | FK → Employee |
| Month | int | Tháng |
| Year | int | Năm |
| Status | string | Draft / Confirmed / Paid / Rejected |

### Dữ liệu chấm công

| Field | Type | Mô tả |
|-------|------|--------|
| StandardWorkingDays | int | Ngày công chuẩn |
| ActualWorkingDays | double | Ngày công thực tế |
| PaidLeaveDays | double | Ngày phép có lương |
| UnpaidLeaveDays | double | Nghỉ không lương |
| OvertimeHours | double | Giờ OT |
| LateCount | int | Số lần đi trễ |

### Tính lương

| Field | Type | Mô tả |
|-------|------|--------|
| BaseSalary | decimal | Lương cơ bản |
| TotalAllowances | decimal | Tổng phụ cấp |
| OvertimePay | decimal | Tiền OT |
| Bonus | decimal | Thưởng |
| GrossIncome | decimal | Tổng thu nhập |
| EmployeeBHXH | decimal | BHXH (8%) |
| EmployeeBHYT | decimal | BHYT (1.5%) |
| EmployeeBHTN | decimal | BHTN (1%) |
| TaxableIncome | decimal | Thu nhập chịu thuế |
| PersonalIncomeTax | decimal | Thuế TNCN |
| TotalDeductions | decimal | Tổng khấu trừ |
| PreviousDebt | decimal | Nợ tháng trước |
| FinalNetSalary | decimal | Lương thực nhận |

### Embedded: EmployeeSnapshot (immutable)

| Field | Type | Mô tả |
|-------|------|--------|
| FullName | string | Tên NV tại thời điểm tính |
| EmployeeCode | string | Mã NV |
| DepartmentId | string | Phòng ban |
| PositionId | string | Chức vụ |
| BaseSalary | decimal | Lương cơ bản |

---

## ⚙️ Common Entities

### AuditLog (Collection: audit_logs)

| Field | Type | Mô tả |
|-------|------|--------|
| UserId | string | Người thao tác |
| UserName | string | Tên người thao tác |
| Action | string | Create / Update / Delete |
| TableName | string | Bảng bị tác động |
| RecordId | string | ID bản ghi |
| OldValues | string? | JSON string |
| NewValues | string? | JSON string |

### SystemSetting (Collection: system_settings)

| Field | Type | Mô tả |
|-------|------|--------|
| Key | string | Tên setting |
| Value | string | Giá trị |
| Description | string | Mô tả |
| Group | string | Payroll / Attendance... |

---

## 🔗 Entity Relationships

- **Employee ↔ User**: 1:1 (qua EmployeeId)
- **Employee → Contract**: 1:N
- **Employee → LeaveRequest**: 1:N
- **Employee → AttendanceBucket**: 1:N (1 per month)
- **Employee → Payroll**: 1:N (1 per month)
- **Employee → Department**: N:1
- **Employee → Position**: N:1
- **Department → Department**: Self-referencing (parent-child)
- **Position → Position**: Self-referencing (parent-child)
- **Department → Employee**: N:1 (Manager)
- **Position → Department**: N:1
- **LeaveType → LeaveAllocation**: 1:N
- **Contract → LeaveAllocation**: triggers initialization
- **Shift → DailyLog**: via ShiftCode
