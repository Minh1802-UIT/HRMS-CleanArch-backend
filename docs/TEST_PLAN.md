# TEST PLAN
# Kế Hoạch Kiểm Thử — EmployeeCleanArch HRM

---

## 1. TỔNG QUAN

### Mục tiêu
- Đảm bảo tất cả business rules hoạt động đúng
- Phát hiện và fix 6 BUG đã xác định
- Regression testing khi thêm tính năng mới

### Phạm vi

| Module | Test Level |
|--------|-----------|
| Auth | Unit + Integration |
| HR (Employee, Contract) | Unit + Integration |
| Attendance | Unit + Integration |
| Leave | Unit + Integration |
| Payroll | Unit (CRITICAL) |
| Organization | Integration |

### Công cụ

| Tool | Mục đích |
|------|---------|
| xUnit | Test framework |
| Moq / NSubstitute | Mocking |
| FluentAssertions | Assert dễ đọc |
| Swagger UI | Manual API testing |
| Postman | API collection testing |

---

## 2. BUG FIX TEST CASES (Priority 1)

### 🔴 BUG-1: BHXH/BHYT/BHTN tính sai

**File**: `PayrollProcessingService.cs`

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-BUG1-01 | BH tính trên baseSalary | baseSalary = 10M, grossIncome = 15M | BHXH = 800K (8% × 10M) | [ ] |
| TC-BUG1-02 | BH có mức trần | baseSalary = 100M, cap = 36M | BHXH = 2.88M (8% × 36M) | [ ] |
| TC-BUG1-03 | baseSalary = 0 | baseSalary = 0 | BHXH = 0 | [ ] |
| TC-BUG1-04 | baseSalary < 0 | baseSalary = -1 | Throw validation error | [ ] |

---

### 🔴 BUG-2: TotalDeductions luôn = 0

**File**: `PayrollProcessingService.cs`

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-BUG2-01 | TotalDeductions được tính | BHXH=800K, BHYT=150K, BHTN=100K, Tax=500K | TotalDeductions = 1.55M | [ ] |
| TC-BUG2-02 | TotalDeductions có debt | Trên + PreviousDebt=200K | TotalDeductions = 1.75M | [ ] |
| TC-BUG2-03 | Không có deductions | All = 0 | TotalDeductions = 0 | [ ] |

---

### 🔴 BUG-3: Leave không check balance

**File**: `LeaveRequestService.cs`

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-BUG3-01 | Đủ balance | Balance = 10, Request = 3 | Tạo thành công | [ ] |
| TC-BUG3-02 | Không đủ balance | Balance = 2, Request = 5 | Throw "Insufficient balance" | [ ] |
| TC-BUG3-03 | Balance = 0 | Balance = 0, Request = 1 | Throw "Insufficient balance" | [ ] |
| TC-BUG3-04 | Request đúng bằng balance | Balance = 5, Request = 5 | Tạo thành công | [ ] |

---

### 🔴 BUG-4: Sửa đơn đã Approved

**File**: `LeaveRequestService.cs`

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-BUG4-01 | Sửa đơn Pending | Status = Pending | Update thành công | [ ] |
| TC-BUG4-02 | Sửa đơn Approved | Status = Approved | Throw "Cannot modify" | [ ] |
| TC-BUG4-03 | Sửa đơn Rejected | Status = Rejected | Throw "Cannot modify" | [ ] |

---

### 🔴 BUG-5: OT Team Summary

**File**: `AttendanceService.cs`

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-BUG5-01 | OT từ DailyLog | DailyLogs: [OT=1.5, OT=2.0] | TotalOT = 3.5 | [ ] |
| TC-BUG5-02 | Không có OT | DailyLogs: [OT=0, OT=0] | TotalOT = 0 | [ ] |
| TC-BUG5-03 | WorkingHours > 8 nhưng OT = 0 | WorkingHours=9, OT=0 (no shift) | TotalOT = 0 (không dùng WH-8) | [ ] |

---

### 🔴 BUG-6: Register email trùng

**File**: `RegisterCommandHandler.cs`

| # | Test Case | Input | Expected | Status |
|---|-----------|-------|----------|--------|
| TC-BUG6-01 | Email unique | email = "new@test.com" | Register thành công | [ ] |
| TC-BUG6-02 | Email trùng | email = "existing@test.com" | Throw "Email already exists" | [ ] |
| TC-BUG6-03 | Email khác case | "A@Test.com" vs "a@test.com" | Throw (case-insensitive) | [ ] |

---

## 3. MODULE TEST CASES

### 3A. Authentication Module

| # | Test Case | Expected | Priority |
|---|-----------|----------|----------|
| TC-AUTH-01 | Login bằng username đúng | Trả về JWT token | P0 |
| TC-AUTH-02 | Login bằng email đúng | Trả về JWT token | P0 |
| TC-AUTH-03 | Login sai password | Throw "Invalid credentials" | P0 |
| TC-AUTH-04 | Login user bị khóa | Throw "Account disabled" | P0 |
| TC-AUTH-05 | Register username trùng | Throw "Username exists" | P0 |
| TC-AUTH-06 | JWT chứa đủ claims | Token có UserId, EmployeeId, Roles | P0 |
| TC-AUTH-07 | Assign role | User có thêm role mới | P1 |
| TC-AUTH-08 | Change password (đúng old pass) | Thành công | P1 |
| TC-AUTH-09 | Change password (sai old pass) | Throw error | P1 |

### 3B. Employee Module

| # | Test Case | Expected | Priority |
|---|-----------|----------|----------|
| TC-EMP-01 | Tạo NV với data hợp lệ | Thành công + publish Event | P0 |
| TC-EMP-02 | Tạo NV code trùng | Throw "Duplicate code" | P0 |
| TC-EMP-03 | Tạo NV tuổi < 18 | Throw "Under age" | P0 |
| TC-EMP-04 | Tạo NV DepartmentId không tồn tại | Throw "Department not found" | P0 |
| TC-EMP-05 | Auto tạo User khi tạo NV | User record tạo thành công | P0 |
| TC-EMP-06 | Xem NV — role Employee | BankDetails = null | P0 |
| TC-EMP-07 | Xem NV — role Admin | BankDetails hiển thị | P0 |
| TC-EMP-08 | Xóa NV đang là Manager phòng ban | Throw "Cannot delete" | P1 |
| TC-EMP-09 | Danh sách NV phân trang | Đúng page size, total count | P1 |

### 3C. Contract Module

| # | Test Case | Expected | Priority |
|---|-----------|----------|----------|
| TC-CT-01 | Tạo HĐ hợp lệ | Thành công + publish Event | P0 |
| TC-CT-02 | Tạo HĐ StartDate > EndDate | Throw validation error | P0 |
| TC-CT-03 | Tạo HĐ BaseSalary = 0 | Throw validation error | P0 |
| TC-CT-04 | Tạo HĐ mới → HĐ cũ Expired | Old contract.Status = Expired | P0 |
| TC-CT-05 | Tạo HĐ overlap thời gian | Throw "Overlap" | P0 |
| TC-CT-06 | Tạo HĐ → Init Leave Allocation | LeaveAllocation records created | P0 |
| TC-CT-07 | Terminate HĐ | Status = Terminated | P1 |
| TC-CT-08 | Audit log ghi đúng old/new | AuditLog record đúng | P1 |

### 3D. Attendance Module

| # | Test Case | Expected | Priority |
|---|-----------|----------|----------|
| TC-ATT-01 | Process log — Present | Status = "Present" | P0 |
| TC-ATT-02 | Process log — Late | Status = "Late", LateMinutes > 0 | P0 |
| TC-ATT-03 | Process log — Overtime | OT > 0 khi checkout > shift end + 15min | P0 |
| TC-ATT-04 | Process log — No CheckOut (Ghost) | Xử lý graceful, không crash | P0 |
| TC-ATT-05 | Break deduction | WorkingHours trừ break overlap | P0 |
| TC-ATT-06 | Overnight shift | Tính đúng khi shift qua ngày | P1 |
| TC-ATT-07 | Bucket — update existing month | DailyLog thêm/cập nhật đúng | P1 |
| TC-ATT-08 | Recalculate totals | TotalPresent, TotalLateMinutes đúng | P1 |

### 3E. Leave Module

| # | Test Case | Expected | Priority |
|---|-----------|----------|----------|
| TC-LV-01 | Tạo đơn hợp lệ | Status = Pending | P0 |
| TC-LV-02 | Approve → trừ UsedDays | UsedDays tăng, Balance giảm | P0 |
| TC-LV-03 | Reject → không trừ | UsedDays không thay đổi | P0 |
| TC-LV-04 | Cancel Pending | Status = Cancelled | P0 |
| TC-LV-05 | Cancel Approved → hoàn phép | UsedDays giảm | P1 |
| TC-LV-06 | Monthly accrual | AccruedDays tăng đúng rate | P1 |
| TC-LV-07 | Accrual idempotent | Chạy 2 lần cùng tháng → không tăng 2x | P0 |
| TC-LV-08 | Init allocation khi tạo Contract | Allocation cho tất cả LeaveType active | P0 |

### 3F. Payroll Module (CRITICAL)

| # | Test Case | Expected | Priority |
|---|-----------|----------|----------|
| TC-PAY-01 | Tính lương đầy đủ | NetSalary = Gross - BH - Tax - Debt | P0 |
| TC-PAY-02 | Prorated salary (NV nghỉ) | Salary × (ActualDays / StandardDays) | P0 |
| TC-PAY-03 | Thuế TNCN bậc 1 | Income ≤ 5M → Tax = 5% | P0 |
| TC-PAY-04 | Thuế TNCN bậc 7 | Income > 80M → Tax 35% phần vượt | P0 |
| TC-PAY-05 | Giảm trừ cá nhân | Taxable = Gross - BH - 11M | P0 |
| TC-PAY-06 | Giảm trừ phụ thuộc | Taxable -= dependents × 4.4M | P0 |
| TC-PAY-07 | Taxable < 0 → Tax = 0 | Không tính thuế âm | P0 |
| TC-PAY-08 | Nợ carry-forward | NetSalary < 0 → ghi nợ tháng sau | P0 |
| TC-PAY-09 | OvertimePay tính đúng | OT × rate × hourly wage | P1 |
| TC-PAY-10 | EmployeeSnapshot lưu đúng | Snapshot match NV data hiện tại | P0 |
| TC-PAY-11 | Không có HĐ Active | Throw error hoặc skip | P0 |

### 3G. Organization Module

| # | Test Case | Expected | Priority |
|---|-----------|----------|----------|
| TC-ORG-01 | Tạo Department | Thành công | P1 |
| TC-ORG-02 | Tạo Department con | ParentId đúng | P1 |
| TC-ORG-03 | Cycle detection | A→B→C→A → Throw error | P0 |
| TC-ORG-04 | Self-reference | A.ParentId = A.Id → Throw | P0 |
| TC-ORG-05 | Department tree JSON | Đúng cấu trúc phân cấp | P1 |
| TC-ORG-06 | Cache invalidation | Sau CRUD → cache bị xóa | P1 |

---

## 4. INTEGRATION TEST CASES

### API Endpoint Tests (Swagger/Postman)

| # | Test Case | Method | Path | Expected |
|---|-----------|--------|------|----------|
| TC-API-01 | Login thành công | POST | /api/auth/login | 200 + token |
| TC-API-02 | Truy cập không có token | GET | /api/employees/{id} | 401 |
| TC-API-03 | Employee access payroll | GET | /api/payrolls | 403 |
| TC-API-04 | Admin tạo NV | POST | /api/employees | 200 |
| TC-API-05 | HR tạo HĐ | POST | /api/contracts | 200 |
| TC-API-06 | Employee check-in | POST | /api/attendance/check-in | 200 |
| TC-API-07 | Employee xin phép | POST | /api/leaves | 200 |
| TC-API-08 | Manager duyệt phép | PUT | /api/leaves/{id}/review | 200 |
| TC-API-09 | HR tính lương | POST | /api/payrolls/generate | 200 |
| TC-API-10 | Validation fail | POST | /api/employees (empty body) | 400 |

---

## 5. END-TO-END SCENARIOS

### Scenario 1: Employee Lifecycle

```
1. Admin tạo Employee (EMP-001)
2. → Auto tạo User account
3. HR tạo Contract cho EMP-001
4. → Auto init Leave Allocations
5. EMP-001 login → chấm công (Check-In/Out)
6. HR process attendance logs
7. EMP-001 xin nghỉ → Manager duyệt
8. HR tính lương cuối tháng
9. Verify: NetSalary, LeaveBalance, AttendanceBucket đều đúng
```

### Scenario 2: Payroll Accuracy

```
1. Tạo NV với BaseSalary = 15,000,000
2. Tạo Contract Active
3. NV làm 20/22 ngày, nghỉ phép 2 ngày
4. Tính lương:
   - ProRated = 15M × (20/22) = 13,636,364
   - Allowances = sum all
   - GrossIncome = ProRated + Allowances + OT
   - BHXH = 8% × min(15M, cap) = 1,200,000
   - BHYT = 1.5% × min(15M, cap) = 225,000
   - BHTN = 1% × min(15M, cap) = 150,000
   - Taxable = Gross - BH - 11M
   - Tax = Progressive 7-tier
   - Net = Gross - BH - Tax - Debt
5. Verify mỗi thành phần
```

### Scenario 3: Leave Integrity

```
1. NV có Balance = 12 ngày Annual Leave
2. Xin 3 ngày → Approved → Balance = 9
3. Xin 5 ngày → Approved → Balance = 4
4. Xin 5 ngày → Rejected (Balance = 4 < 5)
5. Cancel đơn #2 (5 ngày) → Balance = 9
6. Monthly accrual (+1) → Balance = 10
7. Verify tất cả transitions đúng
```

---

## 6. TEST EXECUTION PLAN

### Phase 1: Bug Fix Tests (tuần 1-2)

- [ ] TC-BUG1-01 → TC-BUG1-04 (Payroll BH)
- [ ] TC-BUG2-01 → TC-BUG2-03 (TotalDeductions)
- [ ] TC-BUG3-01 → TC-BUG3-04 (Leave balance)
- [ ] TC-BUG4-01 → TC-BUG4-03 (Leave update)
- [ ] TC-BUG5-01 → TC-BUG5-03 (OT Summary)
- [ ] TC-BUG6-01 → TC-BUG6-03 (Email duplicate)

### Phase 2: Module Tests (tuần 3-4)

- [ ] TC-AUTH-01 → TC-AUTH-09
- [ ] TC-EMP-01 → TC-EMP-09
- [ ] TC-CT-01 → TC-CT-08
- [ ] TC-ATT-01 → TC-ATT-08
- [ ] TC-LV-01 → TC-LV-08
- [ ] TC-PAY-01 → TC-PAY-11
- [ ] TC-ORG-01 → TC-ORG-06

### Phase 3: Integration + E2E (tuần 5)

- [ ] TC-API-01 → TC-API-10
- [ ] Scenario 1: Employee Lifecycle
- [ ] Scenario 2: Payroll Accuracy
- [ ] Scenario 3: Leave Integrity

---

## 7. ACCEPTANCE CRITERIA (Tiêu Chí Nghiệm Thu)

| Tiêu chí | Mức yêu cầu |
|---------|-------------|
| Bug Fix Tests | 100% PASS (6/6 bugs) |
| Unit Tests (P0) | 100% PASS |
| Unit Tests (P1) | ≥ 90% PASS |
| Integration Tests | 100% PASS |
| E2E Scenarios | 100% PASS |
| Code coverage | ≥ 60% (target) |
| No regression | Tất cả test cũ vẫn PASS |
