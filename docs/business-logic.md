# Tài Liệu Nghiệp Vụ — HRMS

Tài liệu này mô tả tất cả các quy tắc nghiệp vụ, công thức tính toán và luồng xử lý mà hệ thống đang áp dụng.

---

## Mục Lục

1. [Quản Lý Nhân Viên](#1-quản-lý-nhân-viên)
2. [Hợp Đồng & Cấu Phần Lương](#2-hợp-đồng--cấu-phần-lương)
3. [Phòng Ban & Chức Vụ](#3-phòng-ban--chức-vụ)
4. [Ca Làm Việc (Shift)](#4-ca-làm-việc-shift)
5. [Chấm Công — Thu Thập & Xử Lý Log](#5-chấm-công--thu-thập--xử-lý-log)
6. [Chấm Công — Tính Toán Ngày Công](#6-chấm-công--tính-toán-ngày-công)
7. [Nghỉ Phép — Loại Nghỉ & Phân Bổ](#7-nghỉ-phép--loại-nghỉ--phân-bổ)
8. [Nghỉ Phép — Đơn Nghỉ & Duyệt](#8-nghỉ-phép--đơn-nghỉ--duyệt)
9. [Chu Kỳ Lương (Payroll Cycle)](#9-chu-kỳ-lương-payroll-cycle)
10. [Tính Lương (Payroll Calculation)](#10-tính-lương-payroll-calculation)
11. [Thuế Thu Nhập Cá Nhân (PIT)](#11-thuế-thu-nhập-cá-nhân-pit)
12. [Tuyển Dụng](#12-tuyển-dụng)
13. [Hiệu Suất](#13-hiệu-suất)
14. [Cài Đặt Hệ Thống (System Settings)](#14-cài-đặt-hệ-thống-system-settings)

---

## 1. Quản Lý Nhân Viên

### 1.1 Trạng Thái Nhân Viên

| Trạng thái | Mô tả |
|---|---|
| `Active` | Đang làm việc bình thường |
| `Probation` | Đang trong thời gian thử việc |
| `OnLeave` | Đang nghỉ phép dài hạn |
| `Suspended` | Tạm đình chỉ |
| `Resigned` | Đã nghỉ việc (tự nguyện) |
| `Terminated` | Đã bị chấm dứt hợp đồng |

Nhân viên đang ở trạng thái `Active` hoặc `Probation` mới được:
- Tính lương hàng tháng
- Tích lũy ngày phép
- Chấm công

### 1.2 Soft Delete

Nhân viên bị xóa được đánh dấu `IsDeleted = true`, không bị xóa khỏi database. Dữ liệu lịch sử (lương, chấm công, hợp đồng) được giữ nguyên cho mục đích kiểm toán.

### 1.3 Optimistic Concurrency

Mỗi document nhân viên có trường `Version` (số int tăng dần). Khi cập nhật, hệ thống so sánh `Version` trong request với `Version` trong DB — nếu khác nhau → `409 Conflict`.

---

## 2. Hợp Đồng & Cấu Phần Lương

### 2.1 Loại Hợp Đồng

| Loại | Mô tả |
|---|---|
| `FixedTerm` | Hợp đồng có thời hạn xác định |
| `Indefinite` | Hợp đồng không xác định thời hạn |
| `Probation` | Hợp đồng thử việc |
| `Freelance` | Hợp đồng cộng tác viên |

### 2.2 Cấu Phần Lương

Mỗi hợp đồng lưu object `SalaryComponents`:

| Trường | Ý nghĩa |
|---|---|
| `BasicSalary` | Lương cơ bản (dùng làm mẫu số tính ngày công và cơ sở đóng bảo hiểm) |
| `TransportAllowance` | Phụ cấp đi lại |
| `LunchAllowance` | Phụ cấp bữa ăn |
| `OtherAllowance` | Phụ cấp khác |

**Tổng thu nhập gộp ban đầu (GrossIncome) được tính từ tất cả các phần trên** (xem phần 10).

### 2.3 Cảnh Báo Hết Hạn Hợp Đồng

Background service `ContractExpirationBackgroundService` chạy mỗi 24 giờ.
- Nếu hợp đồng có `EndDate` trong vòng **30 ngày** kể từ hôm nay → gửi thông báo in-app cho HR.

---

## 3. Phòng Ban & Chức Vụ

### 3.1 Cây Tổ Chức

- Phòng ban có `ParentId` cho phép tạo cây phân cấp đa cấp.
- Chức vụ gắn với phòng ban và có `SalaryRange` (min/max).

---

## 4. Ca Làm Việc (Shift)

### 4.1 Cấu Hình Ca

| Trường | Mô tả |
|---|---|
| `StartTime` | Giờ bắt đầu ca (TimeSpan, e.g. 08:00) |
| `EndTime` | Giờ kết thúc ca |
| `BreakStartTime` | Bắt đầu giờ nghỉ trưa |
| `BreakEndTime` | Kết thúc giờ nghỉ trưa |
| `GracePeriodMinutes` | Biên độ trễ được tha thứ (mặc định **15 phút**) |
| `OvertimeThresholdMinutes` | Số phút làm thêm tối thiểu trước khi tính OT (mặc định **15 phút**) |
| `IsOvernight` | Ca qua đêm (`EndTime < StartTime`) |
| `StandardWorkingHours` | Số giờ tiêu chuẩn của ca (e.g. 8.0) |

### 4.2 Ca Qua Đêm

Khi `IsOvernight = true`:
```
shiftEnd = log.Date.Add(shift.EndTime).AddDays(1)
```
Checkout xảy ra vào sáng hôm sau vẫn được gắn vào ngày làm việc hôm trước.

### 4.3 Ưu Tiên Áp Dụng Ca

1. Roster (lịch phân công ca đặc biệt theo từng ngày của từng nhân viên).
2. Ca mặc định gắn trong hồ sơ nhân viên (`employee.JobDetails.ShiftId`).

---

## 5. Chấm Công — Thu Thập & Xử Lý Log

### 5.1 Luồng Xử Lý Raw Log

```
Thiết bị chấm công / API check-in
        │
        ▼
RawAttendanceLog (collection)  ← IsProcessed = false
        │
        ▼ (Background Job, mỗi 5 phút)
AttendanceProcessingService.ProcessRawLogsAsync()
        │
        ├─ Group by (EmployeeId + LogicalDate)
        ├─ Merge CheckIn/CheckOut
        ├─ AttendanceCalculator.CalculateDailyStatus()
        └─ AttendanceBucket.AddOrUpdateDailyLog()
```

### 5.2 Logical Date (Ngày Làm Việc Logic)

Hệ thống dùng **day-breaker lúc 06:00 sáng** để gán mỗi punch vào đúng ngày làm việc:

```
LogicalDate = localTime.Hour < 6
              ? localTime.Date.AddDays(-1)   ← thuộc ngày hôm qua
              : localTime.Date               ← thuộc ngày hôm nay
```

**Mục đích:** Nhân viên ca đêm (e.g. 22:00 – 06:00) có checkout vào 06:15 sáng hôm sau → vẫn được tính cho ngày hôm qua.

### 5.3 Loại Raw Log

| Type | Ý nghĩa |
|---|---|
| `CheckIn` | Vào ca, rõ ràng |
| `CheckOut` | Ra ca, rõ ràng |
| `Biometric` | Quẹt thẻ/vân tay không phân biệt vào/ra — hệ thống tự suy luận |

**Quy tắc Biometric:**
- Punch đầu tiên trong ngày = CheckIn (nếu chưa có)
- Punch cuối cùng trong ngày = CheckOut (chỉ khi muộn hơn checkout hiện tại)

### 5.4 Hợp Nhất Punch (Idempotent Merge)

Hệ thống giữ:
- **CheckIn = punch sớm nhất** trong ngày
- **CheckOut = punch muộn nhất** trong ngày

Nếu cùng batch được xử lý lại (retry), kết quả không thay đổi.

### 5.5 Ghost Log (Auto-Close)

Khi nhân viên có `CheckIn` nhưng **không có `CheckOut`** vào cuối ngày hôm sau:

```
AutoCheckOut = ShiftEnd của ngày ghost (giờ địa phương → UTC)
Note = "[Auto-closed] Missing checkout"
IsMissingPunch = true
```

### 5.6 Overnight Checkout Rerouting

Nếu hệ thống nhận `CheckOut` mà ngày logic hiện tại không có `CheckIn`, nhưng ngày logic hôm qua có `CheckIn` của ca qua đêm → checkout tự động được gắn vào ngày hôm qua.

### 5.7 Missing CheckIn Flag

Nếu nhân viên có `CheckOut` nhưng không tìm được `CheckIn` (không phải ca qua đêm):
```
Note = "[Missing] Quên check-in — vui lòng gửi giải trình"
IsMissingCheckIn = true
```

---

## 6. Chấm Công — Tính Toán Ngày Công

### 6.1 Công Thức Trễ (Late)

```
LateThreshold = ShiftStart + GracePeriodMinutes

isLate       = CheckIn > LateThreshold
lateMinutes  = isLate ? (CheckIn - LateThreshold).TotalMinutes : 0
```

> Lưu ý: `lateMinutes` được đo từ **ngưỡng grace period**, không phải từ `ShiftStart`.  
> Ví dụ: GracePeriod = 15 phút, đến muộn 20 phút → `lateMinutes = 5` (không phải 20).

### 6.2 Công Thức Về Sớm (Early Leave)

```
isEarlyLeave      = CheckOut < ShiftEnd
earlyLeaveMinutes = isEarlyLeave ? (ShiftEnd - CheckOut).TotalMinutes : 0
```

### 6.3 Công Thức Tăng Ca (Overtime)

```
otMinutes     = (CheckOut - ShiftEnd).TotalMinutes
overtimeHours = otMinutes >= OvertimeThresholdMinutes
                ? Round(otMinutes / 60.0, 2)
                : 0
```

> OT chỉ được tính khi làm thêm ít nhất `OvertimeThresholdMinutes` (mặc định 15 phút) sau giờ kết thúc ca.

### 6.4 Công Thức Giờ Làm Việc Thực Tế

```
duration       = (CheckOut - CheckIn).TotalHours

breakOverlap   = Max(0, Min(CheckOut, BreakEnd) - Max(CheckIn, BreakStart)).TotalHours

workingHours   = Max(0, duration - breakOverlap)
```

Giờ nghỉ trưa **chỉ bị trừ khi nằm trong khoảng thực sự làm việc** (tức là nhân viên check-in trước BreakStart và checkout sau BreakEnd mới bị trừ đủ).

### 6.5 Tổng Hợp Tháng (AttendanceBucket)

Sau mỗi lần cập nhật `DailyLog`:

```
TotalPresent = count(DailyLogs where Status == Present)
TotalLate    = count(DailyLogs where IsLate == true)
TotalOvertime = sum(DailyLogs.OvertimeHours)
```

### 6.6 Đặc Biệt — Ngày Lễ

Khi ngày làm việc trùng với ngày lễ (public holiday):
- `IsHoliday = true`, ghi tên ngày lễ.
- Hệ thống **không xóa** `WorkingHours` hay `OvertimeHours` — nghỉ lễ vẫn có thể phát sinh OT nếu nhân viên được điều động.

---

## 7. Nghỉ Phép — Loại Nghỉ & Phân Bổ

### 7.1 Thuộc Tính Loại Nghỉ

| Trường | Mô tả |
|---|---|
| `DefaultDaysPerYear` | Số ngày mặc định mỗi năm |
| `IsAccrual` | Có tích lũy theo tháng không |
| `AccrualRatePerMonth` | Số ngày tích lũy mỗi tháng (e.g. 1.0 hoặc 1.25) |
| `AllowCarryForward` | Cho phép chuyển số dư sang năm sau |
| `MaxCarryForwardDays` | Số ngày tối đa được chuyển |
| `IsSandwichRuleApplied` | Áp dụng quy tắc sandwich |

### 7.2 Tích Lũy Hàng Tháng (Accrual)

Background service `LeaveAccrualBackgroundService` chạy mỗi 6 giờ, trigger vào **ngày đầu tiên của tháng**:

```
newAccruedDays = allocation.AccruedDays + leaveType.AccrualRatePerMonth
```

**Điều kiện chống duplicate:**  
`LastAccrualMonth` được lưu để mỗi tháng chỉ tích lũy đúng 1 lần, dù background job chạy nhiều lần.

### 7.3 Số Dư Hiện Tại

```
CurrentBalance = NumberOfDays + AccruedDays - UsedDays
```

### 7.4 Khởi Tạo Khi Nhân Viên Mới

Khi tạo hợp đồng mới cho nhân viên:

| Loại nghỉ | Khởi tạo |
|---|---|
| `IsAccrual = true` | `NumberOfDays = 0`, ngay lập tức gọi `UpdateAccrual(AccrualRatePerMonth)` cho tháng hiện tại |
| `IsAccrual = false` | `NumberOfDays = 1` |

### 7.5 Chuyển Số Dư Cuối Năm (Carry Forward)

Hàm `RunYearEndCarryForwardAsync(fromYear)`:

```
carryDays = Min(allocation.CurrentBalance, leaveType.MaxCarryForwardDays)
```

Cộng `carryDays` vào `NumberOfDays` của allocation năm sau. Chỉ áp dụng cho nhân viên trạng thái **Active / Probation**.

---

## 8. Nghỉ Phép — Đơn Nghỉ & Duyệt

### 8.1 Tính Số Ngày Xin Nghỉ

Phụ thuộc vào cấu hình `IsSandwichRuleApplied` của loại nghỉ:

**Standard (không có Sandwich Rule):**
```
daysRequested = CountWorkingDays(FromDate, ToDate)
              = số ngày Thứ 2 - Thứ 6 trong khoảng [FromDate, ToDate] (inclusive)
```

**Sandwich Rule = true:**
```
daysRequested = CountCalendarDays(FromDate, ToDate)
              = (ToDate - FromDate).TotalDays + 1
```

> **Ví dụ Sandwich Rule:** Nghỉ từ Thứ 6 đến Thứ 2.
> - Không có Sandwich: `daysRequested = 2` (Thứ 6 + Thứ 2)
> - Có Sandwich: `daysRequested = 4` (Thứ 6 + Thứ 7 + CN + Thứ 2)

### 8.2 Kiểm Tra Số Dư Khi Nộp Đơn

```
if (CurrentBalance < daysRequested) → 422 ValidationException
```

**Lưu ý quan trọng:** Số dư được trừ **khi manager duyệt** (không phải khi nộp đơn). Bước nộp đơn chỉ kiểm tra đủ số dư hay không.

### 8.3 Kiểm Tra Trùng Lịch

Khi nộp đơn, hệ thống kiểm tra xem có đơn nghỉ nào đang `Pending` hoặc `Approved` trùng ngày không. Nếu trùng → `409 Conflict`.

### 8.4 Luồng Trạng Thái Đơn Nghỉ

```
[Pending] ──── Duyệt ────→ [Approved] ──── Hủy ────→ [Cancelled]
    │                                                      ↑
    ├──── Từ chối ──→ [Rejected]                          │
    └──── Hủy ────────────────────────────────────────────┘
```

**Khi Approved:**
- `allocation.UsedDays += daysRequested`
- Gửi notification cho nhân viên

**Khi Cancelled (từ trạng thái Approved):**
- `allocation.UsedDays -= daysRequested` (refund)

### 8.5 Quy Tắc Nghiệp Vụ

- Chỉ được sửa đơn khi trạng thái là `Pending`.
- Chỉ được hủy đơn khi trạng thái là `Pending` hoặc `Approved`.
- Đơn đã `Rejected` hoặc `Cancelled` không thể thay đổi.

---

## 9. Chu Kỳ Lương (Payroll Cycle)

### 9.1 Chu Kỳ Lệch (Shifted Payroll Cycle)

Hệ thống dùng **chu kỳ lương lệch** theo cấu hình `PAYROLL_START_DAY` và `PAYROLL_END_DAY`:

| Cài đặt | Mặc định |
|---|---|
| `PAYROLL_START_DAY` | 26 |
| `PAYROLL_END_DAY` | 25 |

```
Chu kỳ tháng T/Y:
  StartDate = ngày PAYROLL_START_DAY của tháng (T-1)
  EndDate   = ngày PAYROLL_END_DAY của tháng T

Ví dụ:
  Chu kỳ 03/2026 → StartDate = 26/02/2026, EndDate = 25/03/2026
  Chu kỳ 01/2026 → StartDate = 26/12/2025, EndDate = 25/01/2026
```

### 9.2 Mẫu Số Ngày Công Chuẩn (StandardWorkingDays)

```
StandardWorkingDays = tổng ngày trong [StartDate, EndDate]
                      trừ ngày cuối tuần (Saturday, Sunday)
                      trừ ngày lễ (PublicHoliday)
```

Giá trị này được tính **một lần khi tạo chu kỳ** và lưu vào DB.  
Dù quy tắc thay đổi sau, con số trong chu kỳ đã tạo **không bao giờ thay đổi**.

### 9.3 Weekly Days Off

Cấu hình `WEEKLY_DAYS_OFF` (mặc định `"6,0"` = Thứ 7 và Chủ Nhật) cho phép tùy chỉnh ngày nghỉ hàng tuần.

---

## 10. Tính Lương (Payroll Calculation)

### 10.1 Tổng Quan Pipeline

```
1. Lấy dữ liệu       → Hợp đồng, Chấm công, Chu kỳ lương, Cài đặt thuế/BHXH
2. Tính thu nhập     → BaseSalary, Allowances, OvertimePay → GrossIncome
3. Tính bảo hiểm     → BHXH, BHYT, BHTN
4. Tính thuế         → Taxable Income → PIT
5. Trừ nợ            → Debt từ tháng trước
6. Thu nhập ròng     → NetSalary, NewDebt
7. Ghi DB            → Chỉ ghi đề nếu trạng thái là Draft (không ghi đề Paid)
```

### 10.2 Công Thức Tính Thu Nhập

**Bước 1 — Lương theo ngày:**
$$
\text{DailyWage} = \frac{\text{BaseSalary} + \text{Allowances}}{\text{StandardWorkingDays}}
$$

**Bước 2 — Lương thực tế theo ngày công:**
$$
\text{SalaryByAttendance} = \text{DailyWage} \times \text{ActualPayableDays}
$$

- `ActualPayableDays` = `TotalPresent` (số ngày Có Mặt trong chu kỳ, lấy từ AttendanceBucket sau khi đã filter theo cửa sổ chu kỳ).

**Bước 3 — Tăng ca:**
$$
\text{HourlyRate} = \frac{\text{BaseSalary}}{\text{StandardWorkingDays} \times 8}
$$

$$
\text{OvertimePay} = \text{OvertimeHours} \times \text{HourlyRate} \times \text{OvertimeRateNormal}
$$

- `OvertimeRateNormal` = **1.5** (mặc định, hệ số nhân lương tăng ca ngày thường).

**Bước 4 — Thu nhập gộp:**
$$
\text{GrossIncome} = \text{SalaryByAttendance} + \text{OvertimePay}
$$

### 10.3 Công Thức Bảo Hiểm Xã Hội

Bảo hiểm tính trên `InsuranceSalary`, được giới hạn bởi trần:

$$
\text{InsuranceSalary} = \min(\text{BaseSalary}, \text{InsuranceSalaryCap})
$$

| Loại | Tỷ lệ phần trăm | Cài đặt hệ thống |
|---|---|---|
| BHXH (Bảo hiểm xã hội) | **8%** | `BHXH_RATE = 0.08` |
| BHYT (Bảo hiểm y tế) | **1.5%** | `BHYT_RATE = 0.015` |
| BHTN (Bảo hiểm thất nghiệp) | **1%** | `BHTN_RATE = 0.01` |

$$
\text{BHXH} = \text{InsuranceSalary} \times 0.08
$$
$$
\text{BHYT} = \text{InsuranceSalary} \times 0.015
$$
$$
\text{BHTN} = \text{InsuranceSalary} \times 0.01
$$

**Trần đóng bảo hiểm (InsuranceSalaryCap):** `36,000,000 VNĐ` (mặc định, theo quy định nhà nước — có thể cập nhật trong system_settings).

### 10.4 Công Thức Thu Nhập Chịu Thuế

$$
\text{IncomeBeforeTax} = \text{GrossIncome} - (\text{BHXH} + \text{BHYT} + \text{BHTN})
$$

$$
\text{PersonalDeductionTotal} = \text{PersonalDeduction} + (\text{DependentCount} \times \text{DependentDeduction})
$$

$$
\text{TaxableIncome} = \max(0,\ \text{IncomeBeforeTax} - \text{PersonalDeductionTotal})
$$

| Giảm trừ | Giá trị mặc định | Cài đặt |
|---|---|---|
| Bản thân (`PersonalDeduction`) | **11,000,000 VNĐ/tháng** | `PERSONAL_DEDUCTION` |
| Người phụ thuộc (`DependentDeduction`) | **4,400,000 VNĐ/người/tháng** | `DEPENDENT_DEDUCTION` |

### 10.5 Xử Lý Nợ (Debt Carry-Forward)

$$
\text{NetSalary (raw)} = \text{GrossIncome} - (\text{BHXH} + \text{BHYT} + \text{BHTN} + \text{PIT}) - \text{DebtFromPreviousMonth}
$$

```
if (NetSalary < 0):
    NewDebt = |NetSalary|
    NetSalary = 0
else:
    NewDebt = 0
```

Nợ được **chuyển sang tháng sau** để trừ vào kỳ lương tiếp theo.

### 10.6 Chốt Bảng Lương

- Trạng thái `Draft` → Có thể tính lại.
- Trạng thái `Approved` → Không tính lại, chỉ Mark Paid.
- Trạng thái `Paid` → **Không bao giờ ghi đè**, log cảnh báo nếu bị gọi.

---

## 11. Thuế Thu Nhập Cá Nhân (PIT)

Hệ thống áp dụng biểu thuế lũy tiến **7 bậc** theo quy định Việt Nam (TT111/2013/TT-BTC, sửa đổi).

### 11.1 Biểu Thuế 7 Bậc

| Bậc | Thu nhập tính thuế (TaxableIncome) | Thuế suất | Công thức rút gọn |
|---|---|---|---|
| 1 | ≤ 5,000,000 | 5% | `TaxableIncome × 5%` |
| 2 | 5,000,001 – 10,000,000 | 10% | `TaxableIncome × 10% − 250,000` |
| 3 | 10,000,001 – 18,000,000 | 15% | `TaxableIncome × 15% − 750,000` |
| 4 | 18,000,001 – 32,000,000 | 20% | `TaxableIncome × 20% − 1,650,000` |
| 5 | 32,000,001 – 52,000,000 | 25% | `TaxableIncome × 25% − 3,250,000` |
| 6 | 52,000,001 – 80,000,000 | 30% | `TaxableIncome × 30% − 5,850,000` |
| 7 | > 80,000,000 | 35% | `TaxableIncome × 35% − 9,850,000` |

### 11.2 Ví Dụ Tính PIT

**Scenario:** Nhân viên có thu nhập tính thuế = **15,000,000 VNĐ**

```
TaxableIncome = 15,000,000
→ Bậc 3 (10M – 18M)
→ PIT = 15,000,000 × 15% − 750,000
       = 2,250,000 − 750,000
       = 1,500,000 VNĐ
```

**Kiểm chứng từ unit test (`VietnameseTaxCalculatorTests`):**

| TaxableIncome | PIT |
|---|---|
| 0 | 0 |
| 3,000,000 | 150,000 |
| 5,000,000 | 250,000 |
| 7,500,000 | 500,000 |
| 10,000,000 | 750,000 |
| 15,000,000 | 1,500,000 |
| 20,000,000 | 2,350,000 |
| 35,000,000 | 5,500,000 |
| 60,000,000 | 12,150,000 |
| 100,000,000 | 25,150,000 |

---

## 12. Tuyển Dụng

### 12.1 Vòng Trạng Thái Ứng Viên

```
Applied → Screening → Interviewed → Offered → Hired / Rejected
```

### 12.2 Onboarding (Chuyển Ứng Viên → Nhân Viên)

Khi ứng viên được nhận (Hired), hệ thống thực hiện command `OnboardCandidateCommand`:

1. Tạo tài khoản user (ASP.NET Identity) với mật khẩu tạm thời.
2. Tạo entity `Employee` với thông tin từ ứng viên.
3. Cập nhật `Candidate.Status = Hired`.
4. Gắn cờ `RequirePasswordChange = true` → nhân viên phải đổi mật khẩu khi đăng nhập lần đầu.
5. Gửi email chào mừng kèm thông tin đăng nhập.

---

## 13. Hiệu Suất

### 13.1 Goal Tracking

- Mục tiêu (`GoalEntity`) có `Progress` từ 0 – 100%.
- Nhân viên tự cập nhật tiến độ.
- Trạng thái: `NotStarted → InProgress → Completed`.

### 13.2 Review Kỳ

- `PerformanceReview` gắn với `ReviewPeriod` (e.g. Q1/2026).
- Điểm đánh giá `Score` được ghi bởi manager.

---

## 14. Cài Đặt Hệ Thống (System Settings)

Các tham số tính toán được lưu trong collection `system_settings` và có thể thay đổi không cần redeploy:

| Key | Giá trị mặc định | Ý nghĩa |
|---|---|---|
| `PAYROLL_START_DAY` | `26` | Ngày bắt đầu chu kỳ lương |
| `PAYROLL_END_DAY` | `25` | Ngày kết thúc chu kỳ lương |
| `WEEKLY_DAYS_OFF` | `6,0` | Ngày nghỉ hàng tuần (6=Thứ 7, 0=CN) |
| `BHXH_RATE` | `0.08` | Tỷ lệ đóng BHXH (nhân viên) |
| `BHYT_RATE` | `0.015` | Tỷ lệ đóng BHYT (nhân viên) |
| `BHTN_RATE` | `0.01` | Tỷ lệ đóng BHTN (nhân viên) |
| `INSURANCE_SALARY_CAP` | `36000000` | Trần lương tính bảo hiểm (VNĐ) |
| `PERSONAL_DEDUCTION` | `11000000` | Giảm trừ bản thân (VNĐ/tháng) |
| `DEPENDENT_DEDUCTION` | `4400000` | Giảm trừ người phụ thuộc (VNĐ/tháng) |
| `OT_RATE_NORMAL` | `1.5` | Hệ số tăng ca ngày thường |
| `TimezoneId` | `Asia/Ho_Chi_Minh` | Múi giờ cho tất cả tính toán ngày/giờ |

---

## Phụ Lục — Ví Dụ Tính Lương Đầy Đủ

**Giả sử:**
- `BaseSalary` = 15,000,000 VNĐ
- `Allowances` = 2,000,000 VNĐ (transport + lunch)
- `StandardWorkingDays` = 22 ngày (chu kỳ tháng)
- `ActualPayableDays` = 20 ngày (vắng 2 ngày không phép)
- `OvertimeHours` = 4 giờ
- `DependentCount` = 1 người

**Bước 1 — Thu nhập gộp:**
```
DailyWage   = (15,000,000 + 2,000,000) / 22 = 772,727 VNĐ/ngày

HourlyRate  = 15,000,000 / 22 / 8 = 85,227 VNĐ/giờ
OvertimePay = 4 × 85,227 × 1.5 = 511,364 VNĐ

GrossIncome = 772,727 × 20 + 511,364 = 15,454,540 + 511,364 = 15,965,904 VNĐ
```

**Bước 2 — Bảo hiểm:**
```
InsuranceSalary = Min(15,000,000, 36,000,000) = 15,000,000

BHXH = 15,000,000 × 8%  = 1,200,000
BHYT = 15,000,000 × 1.5% =   225,000
BHTN = 15,000,000 × 1%  =   150,000
Tổng BH = 1,575,000
```

**Bước 3 — Thuế:**
```
IncomeBeforeTax    = 15,965,904 − 1,575,000 = 14,390,904

PersonalDeduction  = 11,000,000 + (1 × 4,400,000) = 15,400,000

TaxableIncome = Max(0, 14,390,904 − 15,400,000) = 0

PIT = 0 VNĐ  (thu nhập chịu thuế âm → không đóng thuế tháng này)
```

**Bước 4 — Lương ròng:**
```
NetSalary = 15,965,904 − 1,575,000 − 0 − 0 (no debt)
          = 14,390,904 VNĐ
```
