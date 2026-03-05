# Báo cáo Business Rules: Tạo Nhân Viên & Hợp Đồng

> **Ngày báo cáo:** 05/03/2026  
> **Phạm vi:** Quy trình tạo nhân viên mới, tạo hợp đồng mới và tất cả các quy trình bị ảnh hưởng  
> **Nguồn phân tích:** Mã nguồn `Employee.Domain`, `Employee.Application`

---

## Mục lục

1. [Quy trình tạo nhân viên mới (Direct)](#1-quy-trình-tạo-nhân-viên-mới-direct)
2. [Quy trình tạo nhân viên qua Tuyển dụng (Onboard)](#2-quy-trình-tạo-nhân-viên-qua-tuyển-dụng-onboard)
3. [Quy trình tạo hợp đồng mới](#3-quy-trình-tạo-hợp-đồng-mới)
4. [Máy trạng thái hợp đồng](#4-máy-trạng-thái-hợp-đồng)
5. [Sự kiện & Quy trình phụ thuộc](#5-sự-kiện--quy-trình-phụ-thuộc)
6. [Tác động đến Bảng Lương (Payroll)](#6-tác-động-đến-bảng-lương-payroll)
7. [Tác động đến Nghỉ Phép (Leave)](#7-tác-động-đến-nghỉ-phép-leave)
8. [Quy trình xóa nhân viên (cascade)](#8-quy-trình-xóa-nhân-viên-cascade)
9. [Sơ đồ luồng tổng quan](#9-sơ-đồ-luồng-tổng-quan)
10. [Tổng hợp lỗi ngoại lệ](#10-tổng-hợp-lỗi-ngoại-lệ)

---

## 1. Quy trình tạo nhân viên mới (Direct)

**Handler:** `CreateEmployeeHandler`  
**Command:** `CreateEmployeeCommand`  
**Phân quyền:** `Admin`, `HR`

### 1.1 Các bước xử lý

| Bước | Hành động | Rule / Exception |
|------|-----------|-----------------|
| 1 | Kiểm tra mã nhân viên trùng lặp | `ConflictException` nếu `EmployeeCode` đã tồn tại |
| 2 | Xác minh Phòng ban (DepartmentId) | `NotFoundException` nếu phòng ban không tồn tại |
| 3 | Xác minh Chức vụ (PositionId) | `NotFoundException` nếu chức vụ không tồn tại |
| 4 | Tạo `EmployeeEntity` qua mapper | Gọi Domain constructor |
| 5 | Lưu vào database | Trong transaction |
| 6 | Phát sự kiện `EmployeeCreatedEvent` | Fire-and-forget |
| 7 | Commit transaction | |
| 8 | Xóa cache Employee Lookup | `CacheKeys.EmployeeLookup` |

### 1.2 Validation Rules (FluentValidation)

| Trường | Quy tắc |
|--------|--------|
| `EmployeeCode` | Bắt buộc; tối đa 20 ký tự; chỉ chứa `[A-Z0-9\-_]` (chữ hoa, số, dấu gạch ngang/dưới) |
| `FullName` | Bắt buộc; tối đa 100 ký tự |
| `Email` | Bắt buộc; đúng định dạng email |
| `PersonalInfo.PhoneNumber` | Bắt buộc; định dạng Việt Nam `^0[3-9]\d{8,9}$` (10–11 chữ số, bắt đầu 03–09) |
| `PersonalInfo.DateOfBirth` | Bắt buộc; **tuổi >= 18** tại thời điểm tạo |
| `JobDetails.DepartmentId` | Bắt buộc |
| `JobDetails.PositionId` | Bắt buộc |
| `JobDetails.JoinDate` | Bắt buộc |

### 1.3 Domain Rules (Entity Level)

| Rule | Vị trí kiểm tra |
|------|----------------|
| `EmployeeCode`, `FullName`, `Email` không được rỗng | Constructor `EmployeeEntity` |
| Tuổi >= 18 khi cập nhật PersonalInfo | `UpdatePersonalInfo()` |
| `FullName`, `Email` không được rỗng khi cập nhật | `UpdateBasicInfo()` |

### 1.4 Trạng thái mặc định sau khi tạo

- `JobDetails.Status` = `EmployeeStatus.Probation` *(thử việc)*
- `AvatarUrl` = `null`
- `BankDetails`, `PersonalInfo` = rỗng (cần cập nhật sau)

---

## 2. Quy trình tạo nhân viên qua Tuyển dụng (Onboard)

**Handler:** `OnboardCandidateHandler`  
**Command:** `OnboardCandidateCommand`  
**Phân quyền:** `Admin`, `HR`

### 2.1 Điều kiện tiên quyết – Pipeline Ứng viên

Ứng viên phải trải qua đúng pipeline sau (không thể "nhảy cóc"):

```
Applied → Interviewing → Test → Hired → Onboarded
                       ↘↗                    ↘
                     Rejected (từ mọi bước)
```

| Chuyển trạng thái hợp lệ | Điều kiện |
|--------------------------|-----------|
| `Applied → Interviewing` | ✅ |
| `Applied → Rejected` | ✅ |
| `Interviewing → Test` | ✅ |
| `Interviewing → Hired` | ✅ |
| `Interviewing → Rejected` | ✅ |
| `Test → Hired` | ✅ |
| `Test → Rejected` | ✅ |
| `Hired → Onboarded` | ✅ (chỉ diễn ra qua OnboardCandidate) |
| `Hired → Rejected` | ✅ |
| `Rejected / Onboarded → *` | ❌ **Terminal state** – Không thể thay đổi |

### 2.2 Các bước xử lý

| Bước | Hành động | Rule / Exception |
|------|-----------|-----------------|
| 1 | Tìm ứng viên theo `CandidateId` | `ValidationException` nếu không tìm thấy |
| 2 | Kiểm tra trạng thái ứng viên | `ValidationException` nếu status ≠ `Hired` |
| 3 | Tạo `EmployeeEntity` từ thông tin ứng viên | FullName, Email lấy từ Candidate; DOB lấy từ request |
| 4 | Gán `JobDetails` (Department, Position, Manager, JoinDate) | Status = `Active` *(khác với tạo trực tiếp là Probation)* |
| 5 | Gán `PersonalInfo` (Phone từ Candidate, DOB từ request) | Kiểm tra age >= 18 |
| 6 | Lưu Employee vào database | |
| 7 | Cập nhật trạng thái Candidate → `Onboarded` | |
| 8 | Phát sự kiện `EmployeeCreatedEvent` | Cùng event với Direct Create |
| 9 | Commit transaction | |

> **⚠️ Khác biệt quan trọng so với Direct Create:**
> - Nhân viên qua Onboard có `Status = Active` (không phải `Probation`)
> - Không kiểm tra trùng mã nhân viên (lỗ hổng tiềm ẩn)
> - Không validate `EmployeeCode` format qua FluentValidation strict

---

## 3. Quy trình tạo hợp đồng mới

**Service:** `ContractService.CreateAsync()`  
**Phân quyền:** Không khai báo `[Authorize]` riêng — kế thừa từ endpoint

### 3.1 Các bước xử lý

| Bước | Hành động | Rule / Exception |
|------|-----------|-----------------|
| 1 | Xác minh nhân viên tồn tại | `NotFoundException` nếu `EmployeeId` không tồn tại |
| 2 | Kiểm tra `EndDate >= StartDate` | `ValidationException` nếu vi phạm |
| 3 | Kiểm tra `BasicSalary >= 0` | `ValidationException` nếu lương âm |
| 4 | Tìm hợp đồng Active hiện có của nhân viên | |
| 5 | **Auto-expire logic** | Nếu `NewStartDate > OldStartDate` → Expire hợp đồng cũ |
| 6 | **Overlap check** | `ValidationException` nếu trùng thời gian với hợp đồng Active khác |
| 7 | Tạo hợp đồng mới, gọi `Activate()` ngay | Status: `Draft` → `Active` |
| 8 | Lưu vào database | |
| 9 | Phát sự kiện `ContractCreatedEvent` | |
| 10 | Commit transaction | |

### 3.2 Chi tiết Auto-Expire Logic

Khi tạo hợp đồng mới:

```
Nếu (NewContract.StartDate > OldActiveContract.StartDate):
  → OldActiveContract.EndDate = NewContract.StartDate - 1 ngày
  → OldActiveContract.Status = Expired
```

**Mục đích:** Đảm bảo tại mọi thời điểm, nhân viên chỉ có **1 hợp đồng Active**.

### 3.3 Cấu trúc SalaryComponents (trong hợp đồng)

| Trường | Mô tả | Ghi chú |
|--------|-------|---------|
| `BasicSalary` | Lương cơ bản | **Bắt buộc** (>= 0) |
| `TransportAllowance` | Phụ cấp đi lại | Tùy chọn |
| `LunchAllowance` | Phụ cấp cơm trưa | Tùy chọn |
| `OtherAllowance` | Phụ cấp khác | Tùy chọn |

### 3.4 Cập nhật hợp đồng (UpdateAsync)

- Chỉ cho phép cập nhật **`EndDate`** và **`Salary`**
- Validator Overlap: Nếu thay đổi EndDate → kiểm tra không trùng với hợp đồng khác
- Ghi **Audit Log** sau khi cập nhật

### 3.5 Chấm dứt hợp đồng (TerminateAsync)

| Trạng thái | Có thể Terminate? |
|-----------|-----------------|
| `Active` | ✅ |
| `Draft` | ✅ |
| `Terminated` | ❌ Đã chấm dứt rồi |
| `Expired` | ❌ Không thể terminate hợp đồng hết hạn |

- Khi terminate: `EndDate = DateTimeNow`, `Note = "Manual termination"`
- Ghi **Audit Log**

---

## 4. Máy trạng thái hợp đồng

```
              ┌─────────────────────────────────────────────┐
              │                                             │
              ▼                                             │
           [Draft] ──── Activate() ───► [Active]            │
                                           │                │
                           EndDate < Now   │   Terminate()  │
                        (Scheduled Job)    │                │
                               ▼           ▼                │
                           [Expired]   [Terminated]         │
                                                            │
              ◄─────────── Auto-expire khi tạo hợp đồng mới ┘
```

| Chuyển trạng thái | Method | Điều kiện |
|-------------------|--------|-----------|
| `Draft → Active` | `Activate()` | Chỉ từ Draft |
| `Active → Expired` | `Expire(endDate)` | Chỉ từ Active |
| `Active/Draft → Terminated` | `Terminate(note, date)` | Không áp dụng cho Expired/Terminated |

### Scheduled Job: ExpireContractsCommand

- Chạy theo lịch *(background job)*
- Lấy tất cả hợp đồng `Active` có `EndDate < UtcNow`
- Gọi `Expire(UtcNow)` cho từng hợp đồng
- **Không dùng UnitOfWork transaction** — cập nhật từng record độc lập

---

## 5. Sự kiện & Quy trình phụ thuộc

### 5.1 EmployeeCreatedEvent

**Được phát bởi:** `CreateEmployeeHandler`, `OnboardCandidateHandler`

| Handler | Hành động | Cơ chế |
|---------|-----------|--------|
| `CreateUserEventHandler` | Tạo tài khoản hệ thống (Identity) + gửi email chào mừng với mật khẩu tạm thời | **Fire-and-forget** (async, không block API) |

**Nội dung email:**
- Tên đăng nhập = Email nhân viên
- Mật khẩu tạm thời: tự động sinh ngẫu nhiên
- Yêu cầu đổi mật khẩu lần đăng nhập đầu tiên (`MustChangePassword = true`)

> **⚠️ Lưu ý:** Nếu tạo tài khoản hoặc gửi email thất bại → **Log lỗi, không rollback nhân viên**. Nhân viên vẫn tồn tại nhưng chưa có tài khoản.

### 5.2 ContractCreatedEvent

**Được phát bởi:** `ContractService.CreateAsync()`

| Handler | Hành động |
|---------|-----------|
| `InitializeLeaveOnContractHandler` | Tự động khởi tạo số dư ngày phép cho nhân viên cho năm hiện tại |

**Chi tiết khởi tạo Leave Allocation:**

| Loại phép | IsAccrual | Khởi tạo |
|-----------|-----------|---------|
| Annual Leave (Phép năm) | `true` | `NumberOfDays = 0` + tích lũy 1 tháng đầu theo `AccrualRatePerMonth` |
| Sick Leave (Phép bệnh) | `false` | `NumberOfDays = 1` |
| Các loại khác không tích lũy | `false` | `NumberOfDays = 1` |

- Nếu allocation đã tồn tại với `NumberOfDays > 0` → **bỏ qua** (idempotent)
- Sử dụng **Bulk Upsert** để tối ưu hiệu suất

### 5.3 EmployeeUpdatedEvent

**Handler:** `EmployeeUpdatedEventHandler`  
→ Ghi **Audit Log** (userId, userName, action = `UPDATE_EMPLOYEE`, old/new values)

### 5.4 EmployeeDeletedEvent

**Handler:** `EmployeeDeletedEventHandler`  
→ Xem chi tiết tại [Mục 8](#8-quy-trình-xóa-nhân-viên-cascade)

---

## 6. Tác động đến Bảng Lương (Payroll)

### 6.1 Điều kiện để nhân viên được tính lương

Nhân viên **bị BỎ QUA** trong lần tính lương nếu:
- Không có hợp đồng `Active` tại thời điểm tính
- Hợp đồng không có `BasicSalary` hợp lệ trong `SalaryMap`

> **Kết luận:** Tạo hợp đồng `Active` là **điều kiện bắt buộc** để nhân viên nhận lương.

### 6.2 Công thức tính lương

```
GrossIncome = (BasicSalary + Allowances) / StandardWorkingDays × ActualPayableDays + OvertimePay

OvertimePay = OvertimeHours × (BasicSalary / StandardWorkingDays / 8) × OvertimeRate

InsuranceSalary = MIN(BasicSalary, InsuranceSalaryCap)
BHXH = InsuranceSalary × 8%    (mặc định)
BHYT = InsuranceSalary × 1.5%  (mặc định)
BHTN = InsuranceSalary × 1%    (mặc định)

TaxableIncome = MAX(0, GrossIncome - BHXH - BHYT - BHTN - PersonalDeduction - DependentCount × DependentDeduction)
PIT = Lũy tiến theo biểu thuế TNCN

NetSalary = GrossIncome - BHXH - BHYT - BHTN - PIT - Debt
```

### 6.3 Hằng số hệ thống (System Settings)

| Key | Mặc định | Mô tả |
|-----|---------|-------|
| `BHXH_RATE` | 8% | Tỷ lệ bảo hiểm xã hội |
| `BHYT_RATE` | 1.5% | Tỷ lệ bảo hiểm y tế |
| `BHTN_RATE` | 1% | Tỷ lệ bảo hiểm thất nghiệp |
| `INSURANCE_SALARY_CAP` | 36,000,000 VNĐ | Mức trần bảo hiểm |
| `PERSONAL_DEDUCTION` | 11,000,000 VNĐ | Giảm trừ bản thân |
| `DEPENDENT_DEDUCTION` | 4,400,000 VNĐ/người | Giảm trừ người phụ thuộc |
| `OT_RATE_NORMAL` | 1.5× | Hệ số tăng ca bình thường |

### 6.4 Quy tắc không ghi đè

- Bảng lương đã ở trạng thái `Paid` → **không tính lại**, chỉ tính bảng lương `Draft`

---

## 7. Tác động đến Nghỉ Phép (Leave)

### 7.1 Tạo đơn nghỉ phép (CreateLeaveRequest)

**Điều kiện tiên quyết:** Phải có `LeaveAllocation` (số dư ngày phép) hợp lệ

| Bước | Rule |
|------|------|
| Kiểm tra overlap | Không được có đơn phép khác (Pending/Approved) trong cùng khoảng thời gian → `ConflictException` |
| Kiểm tra số dư | `RemainingDays >= DaysRequested` → `ValidationException` nếu không đủ |
| Tính số ngày yêu cầu | Nếu `IsSandwichRuleApplied = true` → đếm **cả ngày cuối tuần**; ngược lại chỉ đếm **ngày làm việc** |
| Trừ số dư | **KHÔNG trừ khi submit** — chỉ trừ khi **Approved** |

### 7.2 Tích lũy phép năm (Monthly Accrual)

- Chạy **hàng tháng** (background job)
- Chỉ áp dụng cho loại phép có `IsAccrual = true` (ví dụ: Annual Leave)
- Cộng `AccrualRatePerMonth` vào `AccruedDays` của nhân viên Active/Probation
- Không tích lũy nếu tháng đã được xử lý (`LastAccrualMonth == currentMonthKey`)

---

## 8. Quy trình xóa nhân viên (cascade)

**Handler:** `EmployeeDeletedEventHandler`

Khi nhân viên bị xóa, hệ thống tự động:

| Bước | Hành động | Ghi chú |
|------|-----------|---------|
| 1 | Xóa tài khoản Identity (`DeleteUserByEmployeeIdCommand`) | Đồng bộ |
| 2 | Xóa tất cả Contracts của nhân viên | Isolated (lỗi không dừng bước khác) |
| 3 | Xóa tất cả Attendance records | Isolated |
| 4 | Xóa tất cả Raw Attendance Logs | Isolated |
| 5 | Xóa tất cả Leave Requests | Isolated |
| 6 | Xóa tất cả Leave Allocations | Isolated |
| 7 | Xóa tất cả Payroll records | Isolated |
| 8 | Ghi Audit Log (action = `DELETE_EMPLOYEE`) | |

> **Cơ chế:** Mỗi bước xóa được bọc trong `try/catch` riêng. Nếu một bước thất bại, các bước còn lại **vẫn tiếp tục**. Lỗi được thu thập nhưng **không ném ra ngoài**.

---

## 9. Sơ đồ luồng tổng quan

```
┌──────────────────────────────────────────────────────────────────┐
│                    LUỒNG TẠO NHÂN VIÊN                          │
└──────────────────────────────────────────────────────────────────┘

  [Admin/HR]                    [Recruitment]
      │                              │
      ▼                              ▼
CreateEmployeeCommand     OnboardCandidateCommand
  (Direct Creation)         (Candidate must be Hired)
      │                              │
      ▼                              ▼
  Validate:                    Validate:
  - EmployeeCode unique        - Candidate exists
  - Dept/Pos exists            - Status == Hired
  - Age >= 18                  - Age >= 18
  - Code format                     │
      │                              │
      └──────────────────────────────┘
                    │
                    ▼
           EmployeeEntity (Persisted)
                    │
                    ▼
         🔔 EmployeeCreatedEvent
                    │
         ┌──────────┘
         ▼
  CreateUserEventHandler (Fire-and-forget)
  → Create Identity Account
  → Send Welcome Email (temp password)


┌──────────────────────────────────────────────────────────────────┐
│                    LUỒNG TẠO HỢP ĐỒNG                           │
└──────────────────────────────────────────────────────────────────┘

  [Admin/HR]
      │
      ▼
CreateContractDto
      │
      ▼
  Validate:
  - Employee exists
  - EndDate >= StartDate
  - BasicSalary >= 0
  - No date overlap (after auto-expire)
      │
      ▼
  Auto-expire old Active contract
  (if NewStartDate > OldStartDate)
      │
      ▼
  Contract Status: Draft → Active
      │
      ▼
  ContractEntity (Persisted)
      │
      ▼
  🔔 ContractCreatedEvent
      │
      ▼
  InitializeLeaveOnContractHandler
  → Create/Update Leave Allocations (all Leave Types)
     ├── Annual Leave: 0 days + first month accrual
     └── Others (Sick etc.): 1 day


┌──────────────────────────────────────────────────────────────────┐
│               PAYROLL DEPENDENCY ON CONTRACT                     │
└──────────────────────────────────────────────────────────────────┘

  Active Contract → SalaryMap → PayrollProcessingService
  
  Không có hợp đồng Active → Bị BỎ QUA trong tính lương
```

---

## 10. Tổng hợp lỗi ngoại lệ

| Exception | Khi nào xảy ra | Quy trình |
|-----------|---------------|-----------|
| `ConflictException` | EmployeeCode trùng lặp | Create Employee |
| `NotFoundException` | Dept/Pos/Employee/Contract không tìm thấy | Create Employee / Contract |
| `ValidationException` | Lương âm; EndDate < StartDate; Overlap date; Insufficient leave balance | Contract / Leave |
| `ConflictException` | Leave request overlap | Create Leave Request |
| `InvalidOperationException` | Sai luồng trạng thái hợp đồng / ứng viên | Contract Domain / Candidate Domain |
| `ArgumentException` | Field rỗng; Age < 18 | Domain Entity constructor/methods |

---

## 11. Các vấn đề tiềm ẩn & Khuyến nghị

| # | Vấn đề | Mức độ | Khuyến nghị |
|---|--------|--------|-------------|
| 1 | `OnboardCandidate` không validate `EmployeeCode` format (regex) như Direct Create | ⚠️ Medium | Thêm validation cho EmployeeCode trong `OnboardCandidateValidator` |
| 2 | `OnboardCandidate` không kiểm tra trùng `EmployeeCode` | 🔴 High | Thêm `ExistsByCodeAsync` check trong `OnboardCandidateHandler` |
| 3 | `OnboardCandidate` set `Status = Active` trực tiếp thay vì `Probation` | ℹ️ Info | Xem xét xem đây là intentional hay bug (ứng viên được tuyển thường cần thử việc) |
| 4 | `ExpireContractsCommand` không dùng transaction cho batch update | ⚠️ Medium | Wrap trong transaction để đảm bảo atomicity |
| 5 | `EmployeeDeletedEventHandler` không throw nếu các bước cleanup thất bại | ⚠️ Medium | Cân nhắc emit warning event hoặc retry mechanism |
| 6 | `CreateUserEventHandler` fire-and-forget: nếu tạo tài khoản thất bại, nhân viên không có access | ⚠️ Medium | Cân nhắc dead-letter queue hoặc manual retry endpoint |
| 7 | Contract không có validator riêng (FluentValidation) — validation ở Service layer | ℹ️ Info | Nên chuyển validation sang `IValidator<CreateContractDto>` để nhất quán với CQRS pattern |

---

*Báo cáo được tạo tự động từ phân tích mã nguồn dự án EmployeeCleanArch.*
