# BUSINESS RULES — QUY TẮC NGHIỆP VỤ

> Tổng hợp tất cả quy tắc nghiệp vụ, ràng buộc, và luồng xử lý đã triển khai trong hệ thống.

---

## 1. AUTHENTICATION & AUTHORIZATION

| # | Rule | Trạng thái |
|---|------|-----------|
| BR-AUTH-01 | Login hỗ trợ Username HOẶC Email | ✅ Đã implement |
| BR-AUTH-02 | Register: Username phải unique | ✅ Đã implement |
| BR-AUTH-03 | Register: Email phải unique | ❌ Chưa implement |
| BR-AUTH-04 | Register: Gán role mặc định "Employee" | ✅ Đã implement |
| BR-AUTH-05 | Login: Check user bị khóa (IsActive=false) → từ chối | ❌ Chưa implement |
| BR-AUTH-06 | JWT chứa: UserId, EmployeeId, Roles | ✅ Đã implement |
| BR-AUTH-07 | Change Password: Xác thực mật khẩu cũ | ✅ Đã implement |
| BR-AUTH-08 | Refresh Token mechanism | ❌ Chưa implement |
| BR-AUTH-09 | Password policy enforcement (min length, complexity) | ⚠️ Qua Identity config |

---

## 2. ORGANIZATION

| # | Rule | Trạng thái |
|---|------|-----------|
| BR-ORG-01 | Department/Position hỗ trợ phân cấp đệ quy (parent-child) | ✅ |
| BR-ORG-02 | Cycle Detection: Không cho A làm con của chính nó hoặc con-cháu | ✅ |
| BR-ORG-03 | Tree cache bằng Redis, TTL 1 giờ | ✅ |
| BR-ORG-04 | Invalidate cache khi CRUD phòng ban/chức vụ | ✅ |
| BR-ORG-05 | Xóa phòng ban: Check có NV thuộc phòng ban không | ❌ Chưa implement |
| BR-ORG-06 | Xóa chức vụ: Check có NV giữ chức vụ không | ❌ Chưa implement |
| BR-ORG-07 | Xóa phòng ban: Check có phòng ban con không | ❌ Chưa implement |

---

## 3. HUMAN RESOURCE

### 3.1 Employee Rules

| # | Rule | Trạng thái |
|---|------|-----------|
| BR-HR-01 | EmployeeCode phải unique | ✅ |
| BR-HR-02 | Tuổi nhân viên ≥ 18 (validate từ DateOfBirth) | ✅ |
| BR-HR-03 | DepartmentId phải tồn tại | ✅ |
| BR-HR-04 | PositionId phải tồn tại | ✅ |
| BR-HR-05 | Tạo NV → auto tạo User account (event-driven) | ✅ |
| BR-HR-06 | Password auto = random/secure (không hardcode) | ❌ Đang hardcode "Welcome@2025" |
| BR-HR-07 | Xóa NV: Không cho xóa nếu đang là Manager phòng ban | ✅ |
| BR-HR-08 | Xóa NV: Cleanup liên quan (LeaveAlloc, Attendance, Payroll) | ❌ Chưa implement |
| BR-HR-09 | BankDetails ẩn với user không phải Admin/HR/owner | ✅ |
| BR-HR-10 | Transaction: Unit of Work (MongoDB session) | ✅ |

### 3.2 Contract Rules

| # | Rule | Trạng thái |
|---|------|-----------|
| BR-CT-01 | StartDate < EndDate | ✅ |
| BR-CT-02 | BaseSalary > 0 | ✅ |
| BR-CT-03 | Auto-Expire: Tạo HĐ mới → HĐ cũ Active → Expired | ✅ |
| BR-CT-04 | Overlap Check: Không cho 2 HĐ Active trùng thời gian | ✅ |
| BR-CT-05 | Audit Log: Ghi OldValues/NewValues (JSON) | ✅ |
| BR-CT-06 | Tạo HĐ → Publish ContractCreatedEvent → Init Leave | ✅ |
| BR-CT-07 | Terminate: Set Status = Terminated | ✅ |

---

## 4. ATTENDANCE

| # | Rule | Trạng thái |
|---|------|-----------|
| BR-ATT-01 | Timezone: UTC → Local (+7:00 VN) | ✅ |
| BR-ATT-02 | Late = CheckIn > ShiftStart + GracePeriod | ✅ |
| BR-ATT-03 | OT = CheckOut > ShiftEnd + 15min → tính OT | ✅ |
| BR-ATT-04 | WorkingHours = Duration - BreakOverlap | ✅ |
| BR-ATT-05 | Break Deduction: Overlap giữa work time và break time | ✅ |
| BR-ATT-06 | Overnight shift: EndTime + 1 day nếu IsOvernight | ✅ |
| BR-ATT-07 | Ghost Log: CheckIn có, CheckOut không → xử lý graceful | ✅ |
| BR-ATT-08 | Bucket Pattern: 1 document / employee / tháng | ✅ |
| BR-ATT-09 | Concurrency: Lock trên bucket khi update | ✅ |
| BR-ATT-10 | OT trong Team Summary phải dùng DailyLog.OvertimeHours | ❌ Đang tính sai (WorkingHours - 8) |

---

## 5. LEAVE MANAGEMENT

| # | Rule | Trạng thái |
|---|------|-----------|
| BR-LV-01 | Tạo đơn: Check CurrentBalance >= số ngày xin | ❌ Chưa implement |
| BR-LV-02 | Tạo đơn: Check overlap với đơn khác cùng NV | ❌ Chưa implement |
| BR-LV-03 | Sửa đơn: Chỉ cho khi Status = "Pending" | ❌ Chưa implement |
| BR-LV-04 | Hủy đơn: Chỉ cho khi Status = "Pending" | ✅ |
| BR-LV-05 | Approve: Trừ UsedDays trong LeaveAllocation | ✅ |
| BR-LV-06 | Approve: Dùng Unit of Work (transaction) | ✅ |
| BR-LV-07 | Approve: Ghi ApprovedBy | ❌ Chưa implement |
| BR-LV-08 | Accrual: Cộng dồn hàng tháng, check LastAccrualMonth (idempotent) | ✅ |
| BR-LV-09 | Init: ContractCreated → Initialize Allocation cho năm hiện tại | ✅ |
| BR-LV-10 | Cancel đơn Approved → RefundDays | ❌ Chưa implement |
| BR-LV-11 | Sandwich Rule: Nghỉ T6+T2 → mất T7+CN | ⚠️ Entity có flag, chưa implement logic |

---

## 6. PAYROLL

| # | Rule | Trạng thái |
|---|------|-----------|
| BR-PAY-01 | BHXH 8%, BHYT 1.5%, BHTN 1% tính trên **baseSalary** (có mức trần) | ❌ Đang tính trên grossIncome (SAI) |
| BR-PAY-02 | Thuế TNCN: Biểu lũy tiến 7 bậc (Luật VN) | ✅ |
| BR-PAY-03 | Giảm trừ cá nhân: 11,000,000 VNĐ/tháng | ✅ (từ SystemSetting) |
| BR-PAY-04 | Giảm trừ người phụ thuộc: 4,400,000 VNĐ/người/tháng | ✅ (từ SystemSetting) |
| BR-PAY-05 | Nợ carry-forward: Nếu lương âm → ghi nợ tháng sau | ✅ |
| BR-PAY-06 | EmployeeSnapshot: Chụp thông tin NV tại thời điểm tính | ✅ |
| BR-PAY-07 | TotalDeductions = BH + Tax + Debt | ❌ Không được tính (luôn = 0) |
| BR-PAY-08 | Bonus được hỗ trợ trong Entity | ⚠️ Có field nhưng chưa gán giá trị |
| BR-PAY-09 | OT Pay = OvertimeHours × hệ số × lương giờ | ✅ |
| BR-PAY-10 | System Settings dynamic (rates, caps, deductions) | ✅ |

---

## 7. WORKFLOW DIAGRAMS

### 7.1 Employee Lifecycle

```
Tạo Employee
     │
     ├──▶ EmployeeCreatedEvent
     │          │
     │          ├──▶ CreateUserEventHandler (auto tạo user)
     │          └──▶ [Future: Auto tạo Contract]
     │
     ├──▶ Tạo Contract
     │          │
     │          └──▶ ContractCreatedEvent
     │                    │
     │                    └──▶ InitializeLeaveOnContractHandler
     │                              │
     │                              └──▶ LeaveAllocation cho mỗi LeaveType
     │
     ├──▶ Chấm Công hàng ngày
     │          │
     │          └──▶ Process Logs → AttendanceBucket
     │
     ├──▶ Xin Phép → Duyệt → Trừ phép
     │
     ├──▶ Tính Lương cuối tháng
     │          │
     │          └──▶ PayrollEntity + Snapshot
     │
     └──▶ Xóa Employee
               │
               └──▶ EmployeeDeletedEvent (cần handler cleanup)
```

### 7.2 Leave Request Lifecycle

```
Employee tạo đơn
     │
     ▼
  [Pending] ───── Employee sửa đơn
     │                   │
     ├── Employee hủy ──▶ [Cancelled]
     │
     ▼
  Manager Review
     │
     ├── Approve ──▶ [Approved] ──▶ Trừ UsedDays
     │
     └── Reject ───▶ [Rejected]
```

### 7.3 Payroll Lifecycle

```
HR chọn tháng/năm/NV → Generate
     │
     ▼
  [Draft] ──── Sửa/Tính lại
     │
     ▼
  [Confirmed] ── HR xác nhận
     │
     ├── Paid ──▶ [Paid] (đã chi trả)
     │
     └── Reject ──▶ [Rejected] → quay về Draft
```

---

## 8. EVENT-DRIVEN ARCHITECTURE

| Event | Publisher | Handler(s) | Action |
|-------|----------|------------|--------|
| `EmployeeCreatedEvent` | CreateEmployeeHandler | CreateUserEventHandler | Tạo user account |
| `ContractCreatedEvent` | ContractService | InitializeLeaveOnContractHandler | Khởi tạo phép năm |
| `EmployeeDeletedEvent` | DeleteEmployeeHandler | ⚠️ Chưa có handler | Cần cleanup data |
