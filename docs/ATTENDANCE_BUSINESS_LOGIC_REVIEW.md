# Báo Cáo Kiểm Tra Logic Nghiệp Vụ — Attendance Module

> **Ngày**: 05/03/2026  
> **Phạm vi**: `Employee.Application/Features/Attendance/`, `Employee.Domain/Entities/Attendance/`  
> **Reviewer**: GitHub Copilot  

---

## Mục Lục

1. [Tổng Quan Kiến Trúc](#1-tổng-quan-kiến-trúc)
2. [Luồng Xử Lý Chi Tiết](#2-luồng-xử-lý-chi-tiết)
3. [Kiểm Tra Từng Business Rule (BA_05)](#3-kiểm-tra-từng-business-rule-ba_05)
4. [Phân Tích AttendanceCalculator](#4-phân-tích-attendancecalculator)
5. [Phân Tích AttendanceProcessingService](#5-phân-tích-attendanceprocessingservice)
6. [Phân Tích AttendanceService (Query)](#6-phân-tích-attendanceservice-query)
7. [Danh Sách Bug & Gap](#7-danh-sách-bug--gap)
8. [Gợi Ý Sửa Lỗi](#8-gợi-ý-sửa-lỗi)
9. [Tổng Kết & Ưu Tiên](#9-tổng-kết--ưu-tiên)

---

## 1. Tổng Quan Kiến Trúc

### Các File Liên Quan

| File | Vai Trò |
|------|---------|
| `AttendanceCalculator.cs` | Pure function: tính Late/OT/WorkingHours từ Log + Shift |
| `AttendanceProcessingService.cs` | Orchestrator: lấy raw logs → merge → tính → lưu bucket |
| `AttendanceService.cs` | Query: đọc bucket → trả DTO cho API |
| `CheckInHandler.cs` | Command handler: nhận check-in/out → lưu raw log → trigger processing |
| `ShiftService.cs` | CRUD ca làm việc |
| `AttendanceBucket.cs` | Domain entity: bucket 1 tháng / 1 nhân viên |
| `DailyLog.cs` | Value object: log 1 ngày (check-in, check-out, kết quả tính toán) |
| `RawAttendanceLog.cs` | Raw input từ thiết bị chấm công |
| `Shift.cs` | Entity ca làm việc |
| `CheckInCommandValidator.cs` | FluentValidation: validate input check-in |

### Mô Hình Dữ Liệu

```
RawAttendanceLog (collection: raw_attendance_logs)
  ├── EmployeeId
  ├── Timestamp (UTC)
  ├── Type: CheckIn | CheckOut | Biometric
  ├── IsProcessed: bool
  └── ProcessingError: string?

AttendanceBucket (collection: attendance_buckets)
  ├── EmployeeId
  ├── Month: "MM-yyyy"
  ├── DailyLogs: List<DailyLog>
  ├── TotalPresent: int
  ├── TotalLate: int
  └── TotalOvertime: double

DailyLog (embedded trong Bucket)
  ├── Date
  ├── CheckIn (UTC)
  ├── CheckOut (UTC)
  ├── ShiftCode
  ├── WorkingHours, LateMinutes, EarlyLeaveMinutes, OvertimeHours
  ├── Status: Present | Late | EarlyLeave | Absent | Holiday | OnLeave
  ├── IsWeekend, IsHoliday
  └── Note
```

---

## 2. Luồng Xử Lý Chi Tiết

```
[Client] POST /api/attendance/checkin
          │
          ▼
CheckInCommandValidator
  • EmployeeId: NotEmpty
  • Type: phải là "CheckIn" hoặc "CheckOut"
          │
          ▼
CheckInHandler
  ├─ [1] SPAM PROTECTION
  │       GetLatestLogAsync(employeeId)
  │       Nếu diff < 60 giây → throw ConflictException
  │
  ├─ [2] MAP & SAVE
  │       dto.ToRawEntity(employeeId) → RawAttendanceLog (UTC timestamp)
  │       _rawRepo.CreateAsync(rawLog)
  │
  └─ [3] REAL-TIME PROCESSING
          _processingService.ProcessRawLogsAsync()
                │
                ▼
        GetAndLockUnprocessedLogsAsync(batchSize: 50)
                │
                ▼
        Group by (EmployeeId + LocalDate)  ← UTC + 7h offset
        ┌───────────────────────────────────────────────────┐
        │ ProcessSingleGroupAsync(employeeId, workDate, logs)│
        │                                                    │
        │ BeginTransaction (UoW + MongoDB session)           │
        │                                                    │
        │ [A] Ghost Log Resolution                           │
        │     ProcessGhostLogAsync(employeeId, prevDate)     │
        │     Nếu prevDay có CheckIn mà không có CheckOut:   │
        │     → AutoCheckOut = ShiftEnd - 7h offset          │
        │     → Tính lại và lưu                              │
        │                                                    │
        │ [B] GetEffectiveShift                              │
        │     1. GetShiftByDateAsync (roster shift)          │
        │     2. Employee.JobDetails.ShiftId (default shift) │
        │     3. null nếu không có                           │
        │                                                    │
        │ [C] Merge Logs                                     │
        │     CheckIn  = MIN(existing, newCheckIns)          │
        │     CheckOut = MAX(existing, newCheckOuts)         │
        │     Biometric: if !checkIn → first(); checkOut = last() │
        │                                                    │
        │ [D] Defensive Recovery                             │
        │     Nếu checkIn == null sau merge:                 │
        │     → Query toàn bộ raw logs (kể cả đã processed)  │
        │     → Recover earliest CheckIn của ngày            │
        │                                                    │
        │ [E] AttendanceCalculator.CalculateDailyStatus()    │
        │     (chi tiết ở mục 4)                             │
        │                                                    │
        │ [F] Lưu Bucket + Mark logs processed               │
        │     UpdateAsync(bucket)                            │
        │     MarkManyAsProcessedAsync(logIds)               │
        │                                                    │
        │ CommitTransaction                                  │
        └───────────────────────────────────────────────────┘
```

---

## 3. Kiểm Tra Từng Business Rule (BA_05)

### Đối chiếu với tài liệu BA_05_BUSINESS_RULES.md — Mục 4 ATTENDANCE

| Rule | Mô Tả | Trạng Thái BA | Kết Quả Kiểm Tra | Ghi Chú |
|------|--------|--------------|-----------------|---------|
| BR-ATT-01 | Timezone: UTC → Local (+7:00 VN) | ✅ | ⚠️ Cảnh báo | Không nhất quán giữa ProcessingService và Calculator |
| BR-ATT-02 | Late = CheckIn > ShiftStart + GracePeriod | ✅ | ✅ Đúng | Logic chính xác |
| BR-ATT-03 | OT = CheckOut > ShiftEnd + 15 min | ✅ | ✅ Đúng | 15 phút hardcoded, không lấy từ config |
| BR-ATT-04 | WorkingHours = Duration - BreakOverlap | ✅ | ✅ Đúng | Overlap algorithm đúng |
| BR-ATT-05 | Break Deduction: Overlap giữa work time và break time | ✅ | ✅ Đúng | Xử lý break qua đêm (AddDays(1)) |
| BR-ATT-06 | Overnight shift: EndTime + 1 day nếu IsOvernight | ✅ | 🔴 Bug | CheckOut thuộc ngày D+1 bị group sai ngày |
| BR-ATT-07 | Ghost Log: CheckIn có, CheckOut không → xử lý graceful | ✅ | ⚠️ Cảnh báo | Note bị overwrite; chỉ trigger khi có log ngày tiếp theo |
| BR-ATT-08 | Bucket Pattern: 1 document / employee / tháng | ✅ | ✅ Đúng | Key `"MM-yyyy"` |
| BR-ATT-09 | Concurrency: Lock trên bucket khi update | ✅ | ✅ Đúng | GetAndLockUnprocessedLogsAsync + UoW |
| BR-ATT-10 | OT trong Team Summary dùng DailyLog.OvertimeHours | ✅ | ✅ Đúng | `allLogs.Sum(l => l.OvertimeHours)` |

**Kết quả tổng**: 7/10 hoạt động đúng hoàn toàn · 1 bug nghiêm trọng · 2 cảnh báo

---

## 4. Phân Tích AttendanceCalculator

**File**: `Employee.Application/Features/Attendance/Logic/AttendanceCalculator.cs`

### 4.1 Sơ Đồ Logic

```
Input: DailyLog + Shift
        │
        ├─ shift == null hoặc CheckIn == null ?
        │   └─ Present (No Shift) / Absent → return
        │
        ▼
Convert UTC → Local (TimeZoneInfo, DST-aware)
        │
        ▼
Build ShiftStart, ShiftEnd (nếu IsOvernight → +1 day)
        │
        ▼
[LATE CHECK]
  localCheckIn > shiftStart + GracePeriodMinutes?
  └─ Yes → status = Late
           lateMinutes = (localCheckIn - shiftStart)  ← tính từ ShiftStart, không phải threshold
        │
        ▼
[CHECKOUT CHECKS — chỉ nếu có CheckOut]
  ├─ localCheckOut < shiftEnd?
  │   └─ earlyLeaveMinutes = (shiftEnd - localCheckOut)
  │      status = EarlyLeave (chỉ nếu chưa phải Late)
  │
  └─ localCheckOut >= shiftEnd?
      └─ otMinutes = (localCheckOut - shiftEnd)
         overtimeHours = otMinutes >= 15 ? round(otMinutes/60, 2) : 0
        │
        ▼
[WORKING HOURS]
  duration = (localCheckOut - localCheckIn)
  breakOverlap = max(0, min(checkOut,breakEnd) - max(checkIn,breakStart))
  workingHours = max(0, duration - breakOverlap)
        │
        ▼
log.UpdateCalculationResults(workingHours, lateMinutes, earlyLeaveMinutes, overtimeHours, status)
```

### 4.2 Điểm Tốt

- **Thuần túy (pure function)**: Không có side effect, dễ test và reuse.
- **Break deduction đúng**: Dùng overlap algorithm, xử lý break qua đêm (`breakEnd.AddDays(1)`).
- **Overnight shift boundary**: `shiftEnd.AddDays(1)` cho ca đêm.
- **Defensive null checks**: Không crash khi `CheckOut == null`.

### 4.3 Vấn Đề Phát Hiện

#### BUG-04: LateMinutes tính từ ShiftStart thay vì GracePeriod Threshold

```csharp
// Code hiện tại
lateMinutes = (int)(localCheckIn - shiftStart).TotalMinutes;

// Ví dụ:
// ShiftStart = 08:00, GracePeriod = 15 phút, CheckIn = 08:16
// lateMinutes = 16 phút (thực tế chỉ trễ 1 phút sau grace period)

// Cách thường thấy trong HR systems:
// lateMinutes = (int)(localCheckIn - lateThreshold).TotalMinutes; // = 1 phút
```

**Mức độ**: ⚠️ Design ambiguity — tùy quy định công ty. Cần confirm với BA.

#### GAP-01: Không có status kết hợp "Late + EarlyLeave"

```csharp
// Code hiện tại
if (localCheckOut.Value < shiftEnd)
{
  earlyLeaveMinutes = ...; // ghi đúng
  if (status != AttendanceStatus.Late)    // ← nếu đã Late thì SKIP
    status = AttendanceStatus.EarlyLeave;
}
```

Nhân viên vừa đến trễ vừa về sớm → `Status = Late`, `EarlyLeaveMinutes` có giá trị nhưng không phản ánh trên trường `Status`. Báo cáo thống kê bằng `Status` sẽ bỏ sót.

---

## 5. Phân Tích AttendanceProcessingService

**File**: `Employee.Application/Features/Attendance/Services/AttendanceProcessingService.cs`

### 5.1 Điểm Tốt

- **Batch processing**: Lấy tối đa 50 logs mỗi lần, tránh memory flood.
- **Error isolation**: Mỗi group có try/catch riêng — lỗi 1 nhân viên không làm hỏng nhóm khác.
- **Idempotent Merge**: CheckIn lấy MIN, CheckOut lấy MAX → an toàn khi retry.
- **Defensive Recovery**: Tự phục hồi CheckIn bị mất từ raw logs đã processed.
- **Transaction**: Bọc trong MongoDB session, rollback khi lỗi.

### 5.2 Vấn Đề Phát Hiện

---

#### 🔴 BUG-01 (Nghiêm Trọng): Overnight Shift — CheckOut Bị Group Vào Ngày Sai

**Nguyên nhân**: Group key được tính từ LocalDate:

```csharp
Date = (x.Timestamp + _systemOffset).Date
```

**Kịch bản thực tế** — Ca đêm 22:00 → 06:00 (IsOvernight = true):

| Sự kiện | Timestamp (UTC) | Local (+7) | Group Key |
|---------|----------------|------------|-----------|
| CheckIn  | 15:00 UTC ngày D | 22:00 ngày D | **Group D** |
| CheckOut | 23:00 UTC ngày D | 06:00 ngày D+1 | **Group D+1** ← sai! |

**Hậu quả**:
- Group D: `checkIn = 22:00`, `checkOut = null` → **Ghost Log** được tạo
- Group D+1: `checkIn = null`, `checkOut = 06:00` → Defensive Recovery cũng thất bại vì CheckIn của ngày D đã bị `IsProcessed = true`
- Kết quả cuối: Nhân viên ca đêm **không bao giờ có WorkingHours đúng**

**Fix đề xuất**:

```csharp
// Khi group logs, nếu IsOvernight, CheckOut thuộc về nhóm của CheckIn (ngày trước)
// Cần dùng ShiftStart làm mốc, không phải LocalDate của CheckOut timestamp.
// Hoặc: Khi nhận CheckOut timestamp, tra cứu CheckIn pending của employee
// để quyết định gắn vào ngày nào.
```

---

#### ⚠️ BUG-02: Ghost Log Note Bị Overwrite

**Vị trí**: `ProcessGhostLogAsync()` (dòng ~205)

```csharp
// Bước 1: Gán note "Missing Checkout"
prevLog.UpdateCalculationResults(0, 0, 0, 0, AttendanceStatus.Absent,
    "Missing Checkout [System Auto-closed]");

// Bước 2: Gọi Calculator — Calculator gọi UpdateCalculationResults() lại không có note
_calculator.CalculateDailyStatus(prevLog, prevShift);
// → note = "" ← Note bị erase hoàn toàn
```

**Hậu quả**: DailyLog của Ghost Log không có note nào để phân biệt với ngày absent bình thường.

**Fix đề xuất**:

```csharp
_calculator.CalculateDailyStatus(prevLog, prevShift);
// Append note sau khi calculator chạy xong
if (string.IsNullOrEmpty(prevLog.Note))
    prevLog.UpdateCalculationResults(prevLog.WorkingHours, prevLog.LateMinutes,
        prevLog.EarlyLeaveMinutes, prevLog.OvertimeHours, prevLog.Status,
        "Missing Checkout [System Auto-closed]");
```

---

#### ⚠️ BUG-03: Biometric Log Overwrite Explicit CheckOut

**Vị trí**: Merge logic trong `ProcessSingleGroupAsync()` (dòng ~140)

```csharp
// Merge explicit CheckOut → checkOut = MAX(existing, new)    ✅
if (checkOutLogs.Any()) { ... checkOut = newMax; }

// Merge Biometric — LUÔN gán lại checkOut dù checkOut đã có
if (biometricLogs.Any())
{
    if (!checkIn.HasValue) checkIn = allTimes.First();
    checkOut = allTimes.Last();    // ← override explicit CheckOut
}
```

**Kịch bản lỗi**:

| Batch | Type | Timestamp | checkOut sau merge |
|-------|------|-----------|-------------------|
| Cùng 1 batch | CheckOut | 17:30 | 17:30 |
| Cùng 1 batch | Biometric | 17:15 | **17:15** ← sai! |

**Fix đề xuất**:

```csharp
if (biometricLogs.Any())
{
    if (!checkIn.HasValue) checkIn = allTimes.First();
    // Chỉ update checkOut nếu Biometric mới hơn
    var bioLast = allTimes.Last();
    checkOut = checkOut.HasValue ? (bioLast > checkOut.Value ? bioLast : checkOut) : bioLast;
}
```

---

#### ⚠️ GAP-02: Ghost Log Chỉ Resolve Khi Có Log Ngày Tiếp Theo

**Vị trí**: Dòng đầu `ProcessSingleGroupAsync()`:

```csharp
await ProcessGhostLogAsync(employeeId, workDate.AddDays(-1));
```

Ghost Log chỉ được xử lý khi nhân viên đó có **raw log mới** trong ngày tiếp theo. Nếu nhân viên:
- Nghỉ phép D+1
- Không check-in ngày D+1 vì lý do khác

→ Ghost Log của ngày D sẽ **tồn tại vô thời hạn** cho đến khi có raw log tiếp theo.

**Fix đề xuất**: Thêm background/scheduled job (Hangfire hoặc hosted service) chạy hàng đêm lúc 02:00 để sweep tất cả Ghost Logs của ngày hôm trước.

---

#### ⚠️ GAP-03: OT Threshold 15 Phút Hardcoded

**Vị trí**: `AttendanceCalculator.cs` dòng ~75

```csharp
overtimeHours = otMinutes >= 15 ? Math.Round(otMinutes / 60.0, 2) : 0;
```

Không lấy từ `Shift.GracePeriodMinutes` hay `SystemSettings`. Nếu công ty muốn điều chỉnh ngưỡng OT → phải sửa code.

**Fix đề xuất**: Thêm property `OvertimeThresholdMinutes` vào `Shift` entity (default = 15).

---

#### ⚠️ GAP-04: Timezone Không Nhất Quán

| Nơi dùng | Phương pháp | Ghi chú |
|----------|-------------|---------|
| `ProcessingService` (grouping) | `Timestamp + TimeSpan.FromHours(7)` | Cộng cứng 7 giờ |
| `AttendanceCalculator` | `TimeZoneInfo.ConvertTimeFromUtc()` | DST-aware |
| `ProcessGhostLogAsync` (autoCheckOut) | `shiftEndDateTime - _systemOffset` | Cộng cứng 7 giờ |

Với múi giờ VN (không DST) thì không gây lỗi hiện tại, nhưng vi phạm nguyên tắc Single Source of Truth. Cần thống nhất dùng `TimeZoneInfo`.

---

## 6. Phân Tích AttendanceService (Query)

**File**: `Employee.Application/Features/Attendance/Services/AttendanceService.cs`

### 6.1 GetMonthlyAttendanceAsync

- Trả về empty DTO nếu không có bucket — hành vi đúng.

### 6.2 GetMyAttendanceRangeAsync

- Tính tập hợp months cần query — đúng.
- Filter theo `EmployeeId` và `Date` range — đúng.

### 6.3 GetTeamAttendanceSummaryAsync

- Dùng `GetByManagerIdAsync` (BR-ATT-10 verified) — **không** full table scan.
- `EmployeeType` hardcoded = `"Full Time"` — ⚠️ không đọc từ Employee entity.
- `Department` dùng `DepartmentId` thay vì tên phòng ban — ⚠️ UI sẽ hiển thị ID, không phải tên.
- `Office` dùng `PersonalInfo.City` — có thể null, fallback `"N/A"` đã xử lý.

---

## 7. Danh Sách Bug & Gap

### Summary Table

| ID | Mức Độ | Mô Tả | File | Ảnh Hưởng |
|----|--------|--------|------|-----------|
| BUG-01 | 🔴 Nghiêm trọng | Overnight shift: CheckOut bị group sai ngày | `AttendanceProcessingService` | Nhân viên ca đêm không có WorkingHours đúng |
| BUG-02 | ⚠️ Trung bình | Ghost Log note bị overwrite sau khi calculator chạy | `AttendanceProcessingService` | Không thể phân biệt Ghost Log với Absent thường |
| BUG-03 | ⚠️ Trung bình | Biometric log overwrite explicit CheckOut | `AttendanceProcessingService` | Giờ checkout có thể bị ghi sai |
| BUG-04 | ⚠️ Nhỏ | LateMinutes tính từ ShiftStart, không phải GracePeriod threshold | `AttendanceCalculator` | Số phút trễ hiển thị cao hơn thực tế |
| GAP-01 | ⚠️ Trung bình | Không có status kết hợp Late + EarlyLeave | `AttendanceCalculator` | Báo cáo thống kê theo Status bỏ sót |
| GAP-02 | ⚠️ Trung bình | Ghost Log chỉ resolve khi có log ngày tiếp theo | `AttendanceProcessingService` | Ghost Log tồn tại vô thời hạn nếu NV nghỉ liên tục |
| GAP-03 | ⚠️ Nhỏ | OT threshold 15 phút hardcoded | `AttendanceCalculator` | Không configurable theo công ty |
| GAP-04 | ⚠️ Nhỏ | Timezone xử lý không nhất quán (TimeSpan vs TimeZoneInfo) | Cả 2 files | Nguy cơ bug khi đổi timezone |
| GAP-05 | ℹ️ Nhỏ | TeamSummary: EmployeeType hardcoded "Full Time" | `AttendanceService` | Dữ liệu không chính xác |
| GAP-06 | ℹ️ Nhỏ | TeamSummary: Department hiển thị ID thay vì tên | `AttendanceService` | UX kém |

---

## 8. Gợi Ý Sửa Lỗi

### Fix BUG-01: Overnight Shift Group Logic

**Cách tiếp cận**: Khi có CheckOut mà chưa có CheckIn trong ngày hiện tại, kiểm tra xem ngày trước có DailyLog đang **pending CheckOut** không (có CheckIn, IsOvernight shift). Nếu có → gắn CheckOut này vào ngày hôm trước.

```csharp
// Trong ProcessSingleGroupAsync — sau khi merge biometric
// Nếu chỉ có CheckOut và không tìm được CheckIn
if (!checkIn.HasValue && checkOut.HasValue)
{
    // Kiểm tra ngày hôm trước có ghost với overnight shift
    var prevDate = workDate.AddDays(-1);
    var prevBucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(
        employeeId, prevDate.ToString("MM-yyyy"));
    var prevLog = prevBucket?.DailyLogs.FirstOrDefault(x => x.Date.Date == prevDate.Date);

    if (prevLog?.CheckIn.HasValue == true && !prevLog.CheckOut.HasValue)
    {
        var prevShift = await GetEffectiveShiftAsync(employeeId, prevDate);
        if (prevShift?.IsOvernight == true)
        {
            // Gắn CheckOut này vào ngày hôm trước
            prevLog.UpdateCheckTimes(prevLog.CheckIn, checkOut, prevShift.Code);
            _calculator.CalculateDailyStatus(prevLog, prevShift);
            prevBucket!.AddOrUpdateDailyLog(prevLog);
            await _attendanceRepo.UpdateAsync(prevBucket.Id, prevBucket);
            // Không tiếp tục xử lý cho ngày hiện tại
            return;
        }
    }
}
```

---

### Fix BUG-02: Bảo Toàn Ghost Log Note

```csharp
private async Task ProcessGhostLogAsync(string employeeId, DateTime prevDate)
{
    // ... (lấy bucket, prevLog như cũ)

    if (prevLog != null && prevLog.CheckIn.HasValue && !prevLog.CheckOut.HasValue)
    {
        // ... (tính autoCheckOut như cũ)
        prevLog.UpdateCheckTimes(prevLog.CheckIn, autoCheckOut, prevShift.Code);
        _calculator.CalculateDailyStatus(prevLog, prevShift);

        // Append note SAU khi calculator chạy (không bị override nữa)
        prevLog.UpdateCalculationResults(
            prevLog.WorkingHours, prevLog.LateMinutes, prevLog.EarlyLeaveMinutes,
            prevLog.OvertimeHours, prevLog.Status,
            $"[Auto-closed] {prevLog.Note}".Trim()); // giữ note từ calculator + thêm tag

        bucket.AddOrUpdateDailyLog(prevLog);
        await _attendanceRepo.UpdateAsync(bucket.Id, bucket);
    }
}
```

---

### Fix BUG-03: Biometric Không Override Explicit CheckOut

```csharp
if (biometricLogs.Any())
{
    var allTimes = biometricLogs.OrderBy(x => x).ToList();
    if (!checkIn.HasValue)
        checkIn = allTimes.First();

    var bioLastTime = allTimes.Last();
    // Chỉ dùng biometric nếu không có explicit CheckOut, hoặc biometric muộn hơn
    checkOut = checkOut.HasValue
        ? (bioLastTime > checkOut.Value ? bioLastTime : checkOut)
        : bioLastTime;
}
```

---

### Fix GAP-02: Background Sweep Job cho Ghost Logs

```csharp
// Thêm hosted service / Hangfire job
public class GhostLogSweepJob
{
    public async Task ExecuteAsync()
    {
        // 1. Tìm tất cả DailyLogs có CheckIn nhưng không có CheckOut
        //    của ngày hôm qua (hoặc trước đó)
        var yesterday = DateTime.UtcNow.AddHours(7).Date.AddDays(-1);
        var ghostBuckets = await _attendanceRepo.GetBucketsWithGhostLogsAsync(yesterday);

        foreach (var (bucket, log) in ghostBuckets)
        {
            var shift = await GetEffectiveShiftAsync(bucket.EmployeeId, log.Date);
            // Auto-close như ProcessGhostLogAsync hiện tại
        }
    }
}
```

---

### Fix GAP-03: OT Threshold Configurable

```csharp
// Thêm vào Shift entity
public int OvertimeThresholdMinutes { get; private set; } = 15;

// Trong AttendanceCalculator
var otThreshold = shift.OvertimeThresholdMinutes;
overtimeHours = otMinutes >= otThreshold
    ? Math.Round(otMinutes / 60.0, 2)
    : 0;
```

---

### Fix GAP-04: Thống Nhất Timezone

```csharp
// Trong AttendanceProcessingService: thay vì TimeSpan + offset
// Dùng TimeZoneInfo (giống AttendanceCalculator)
private readonly TimeZoneInfo _timeZone =
    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // UTC+7

// Group by
Date = TimeZoneInfo.ConvertTimeFromUtc(x.Timestamp, _timeZone).Date
```

---

## 9. Tổng Kết & Ưu Tiên

### Thống Kê

| Mức Độ | Số Lượng |
|--------|----------|
| 🔴 Bug nghiêm trọng (ảnh hưởng data sai) | 1 |
| ⚠️ Bug trung bình (logic không đúng) | 3 |
| ⚠️ Gap cần bổ sung | 3 |
| ℹ️ Cải tiến nhỏ (UX/config) | 3 |

### Thứ Tự Ưu Tiên Sửa

| Ưu Tiên | Issue | Lý Do |
|---------|-------|--------|
| **P0** | BUG-01 Overnight shift | Gây data sai cho toàn bộ nhân viên ca đêm |
| **P1** | BUG-03 Biometric override | Giờ checkout sai → WorkingHours sai → Payroll sai |
| **P1** | GAP-02 Ghost sweep job | Ghost logs tích lũy gây dữ liệu bẩn |
| **P2** | BUG-02 Ghost Log note | Khó debug, audit trail kém |
| **P2** | GAP-04 Timezone | Technical debt, nguy cơ bug khi scale |
| **P3** | BUG-04 LateMinutes | Xác nhận lại với BA về cách tính |
| **P3** | GAP-01 Late+EarlyLeave | Báo cáo bị thiếu case |
| **P3** | GAP-03 OT threshold | Cần configurable |
| **P4** | GAP-05/06 TeamSummary | UX và data accuracy nhỏ |

### Điểm Mạnh Của Thiết Kế Hiện Tại

- **Bucket Pattern** hợp lý: 1 document/employee/tháng, tối ưu cho MongoDB.
- **AttendanceCalculator là pure function**: Đúng hướng, dễ test.
- **Error isolation per group**: Lỗi 1 nhân viên không ảnh hưởng nhóm khác.
- **Idempotent merge**: An toàn khi retry, tránh duplicate CheckIn/CheckOut.
- **Defensive Recovery**: Tự phục hồi CheckIn bị mất.
- **Unit Tests**: Có coverage cho `AttendanceBucket` và `AttendanceService`.

---

*Generated: 2026-03-05 | Scope: Attendance Module v1.x*
