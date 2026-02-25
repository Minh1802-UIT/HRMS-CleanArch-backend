# ⚖️ Business Rules — Quy Tắc Nghiệp Vụ

---

## 🔐 Authentication Rules

| # | Rule | Status |
|---|------|--------|
| BR-AUTH-01 | Login hỗ trợ Username HOẶC Email | ✅ Done |
| BR-AUTH-02 | Register: Username phải unique | ✅ Done |
| BR-AUTH-03 | Register: Email phải unique | ✅ Done |
| BR-AUTH-04 | Register: Gán role mặc định "Employee" | ✅ Done |
| BR-AUTH-05 | Login: Check user bị khóa → từ chối | ✅ Done |
| BR-AUTH-06 | JWT chứa: UserId, EmployeeId, Roles | ✅ Done |
| BR-AUTH-07 | Change Password: Xác thực mật khẩu cũ | ✅ Done |
| BR-AUTH-08 | Refresh Token mechanism | ✅ Done |

---

## 🏢 Organization Rules

| # | Rule | Status |
|---|------|--------|
| BR-ORG-01 | Dept/Position phân cấp đệ quy (parent-child) | ✅ Done |
| BR-ORG-02 | Cycle Detection khi cập nhật ParentId | ✅ Done |
| BR-ORG-03 | Tree cache Redis, TTL 1 giờ | ✅ Done |
| BR-ORG-04 | Invalidate cache khi CRUD | ✅ Done |
| BR-ORG-05 | Xóa Dept: Check NV thuộc phòng ban | ✅ Done |
| BR-ORG-06 | Xóa Position: Check NV giữ chức vụ | ✅ Done (FIXED) |
| BR-ORG-07 | Xóa Dept: Check phòng ban con | ✅ Done |

---

## 👤 Employee Rules

| # | Rule | Status |
|---|------|--------|
| BR-HR-01 | EmployeeCode phải unique | ✅ Done |
| BR-HR-02 | Tuổi nhân viên ≥ 18 | ✅ Done |
| BR-HR-03 | DepartmentId phải tồn tại | ✅ Done |
| BR-HR-04 | PositionId phải tồn tại | ✅ Done |
| BR-HR-05 | Tạo NV → auto tạo User (event-driven) | ✅ Done |
| BR-HR-06 | Password auto = random/secure | ✅ Done |
| BR-HR-07 | Xóa NV: Block nếu đang là Manager | ✅ Done |
| BR-HR-08 | Xóa NV: Cleanup data liên quan | ✅ Done |
| BR-HR-09 | BankDetails ẩn với non-Admin/HR/owner | ✅ Done |
| BR-HR-10 | Transaction: Unit of Work | ✅ Done |

---

## 📄 Contract Rules

| # | Rule | Status |
|---|------|--------|
| BR-CT-01 | StartDate < EndDate | ✅ Done |
| BR-CT-02 | BaseSalary > 0 | ✅ Done |
| BR-CT-03 | Auto-Expire HĐ cũ khi tạo mới | ✅ Done |
| BR-CT-04 | Overlap Check (2 HĐ Active trùng thời gian) | ✅ Done |
| BR-CT-05 | Audit Log (OldValues/NewValues JSON) | ✅ Done |
| BR-CT-06 | Tạo HĐ → Event → Init Leave | ✅ Done |
| BR-CT-07 | Terminate: Set Status = Terminated | ✅ Done |

---

## ⏰ Attendance Rules

| # | Rule | Status |
|---|------|--------|
| BR-ATT-01 | Timezone: UTC → Local (+7:00 VN) | ✅ Done |
| BR-ATT-02 | Late = CheckIn > ShiftStart + GracePeriod | ✅ Done |
| BR-ATT-03 | OT = CheckOut > ShiftEnd + 15min | ✅ Done |
| BR-ATT-04 | WorkingHours = Duration - BreakOverlap | ✅ Done |
| BR-ATT-05 | Overnight shift: EndTime + 1 day | ✅ Done |
| BR-ATT-06 | Ghost Log: xử lý graceful | ✅ Done |
| BR-ATT-07 | Bucket Pattern: 1 doc / emp / tháng | ✅ Done |
| BR-ATT-08 | Concurrency lock trên bucket | ✅ Done |
| BR-ATT-09 | Team Summary OT dùng DailyLog.OvertimeHours | ✅ Done |

---

## 🌴 Leave Rules

| # | Rule | Status |
|---|------|--------|
| BR-LV-01 | Tạo đơn: Check CurrentBalance >= ngày xin | ✅ Done |
| BR-LV-02 | Tạo đơn: Check overlap với đơn khác | ✅ Done |
| BR-LV-03 | Sửa đơn: Chỉ khi Status = "Pending" | ✅ Done |
| BR-LV-04 | Hủy đơn: Chỉ khi Status = "Pending" | ✅ Done |
| BR-LV-05 | Approve: Trừ UsedDays (LeaveAllocation) | ✅ Done |
| BR-LV-06 | Approve: Unit of Work (transaction) | ✅ Done |
| BR-LV-07 | Approve: Ghi ApprovedBy | ✅ Done |
| BR-LV-08 | Accrual: Cộng dồn hàng tháng, idempotent | ✅ Done |
| BR-LV-09 | Init: ContractCreated → Init Allocation | ✅ Done |
| BR-LV-10 | Cancel Approved → RefundDays | ✅ Done |
| BR-LV-11 | Sandwich Rule logic (Không áp dụng) | ✅ Done |

---

## 💰 Payroll Rules

| # | Rule | Status |
|---|------|--------|
| BR-PAY-01 | BH tính trên baseSalary (có trần) | ✅ Done |
| BR-PAY-02 | Thuế TNCN: Biểu lũy tiến 7 bậc | ✅ Done |
| BR-PAY-03 | Giảm trừ cá nhân: 11,000,000 VNĐ/tháng | ✅ Done |
| BR-PAY-04 | Giảm trừ người phụ thuộc | ✅ Done |
| BR-PAY-05 | Nợ carry-forward | ✅ Done |
| BR-PAY-06 | EmployeeSnapshot | ✅ Done |
| BR-PAY-07 | TotalDeductions = BH + Tax + Debt | ✅ Done |
| BR-PAY-08 | Bonus | ⚠️ Draft |
| BR-PAY-10 | System Settings dynamic | ✅ Done |

---

## 🔄 Event-Driven Architecture

| Event | Publisher | Handler | Action | Status |
|-------|----------|---------|--------|--------|
| EmployeeCreatedEvent | CreateEmployeeHandler | CreateUserEventHandler | Tạo user account | ✅ Done |
| ContractCreatedEvent | ContractService | InitializeLeaveOnContractHandler | Khởi tạo phép năm | ✅ Done |
| EmployeeDeletedEvent | DeleteEmployeeHandler | EmployeeDeletedEventHandler | Cleanup data | ✅ Done |

---

## 🔄 Workflows

### Employee Lifecycle

1. Tạo Employee → EmployeeCreatedEvent → Auto tạo User
2. Tạo Contract → ContractCreatedEvent → Init LeaveAllocation
3. Chấm công hàng ngày → Process Logs → AttendanceBucket
4. Xin phép → Duyệt → Trừ phép
5. Cuối tháng → Tính lương → PayrollEntity + Snapshot
6. Xóa Employee → EmployeeDeletedEvent → Cleanup (chưa có)

### Leave Request Lifecycle

1. Employee tạo đơn → Status = **Pending**
2. Employee có thể sửa/hủy (chỉ khi Pending)
3. Manager review → **Approved** (trừ UsedDays) hoặc **Rejected**

### Payroll Lifecycle

1. HR chọn tháng/năm/NV → Generate → **Draft**
2. HR xác nhận → **Confirmed**
3. Đã chi trả → **Paid** (hoặc Rejected → quay về Draft)
