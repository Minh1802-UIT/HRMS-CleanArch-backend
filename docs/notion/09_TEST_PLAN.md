# 📋 Test Plan — Kế Hoạch Kiểm Thử

---

## 1️⃣ Tổng Quan

### Mục tiêu

- Fix và verify 6 BUG đã xác định
- Đảm bảo business rules hoạt động đúng cho 7 modules
- Regression testing khi thêm feature mới

### Công cụ

| Tool | Mục đích |
|------|---------|
| xUnit | Test framework |
| Moq / NSubstitute | Mocking |
| FluentAssertions | Assert dễ đọc |
| Swagger UI / Postman | API testing |

---

## 2️⃣ Bug Fix Tests (Priority 1)

### 🔴 BUG-1: BHXH/BHYT/BHTN tính sai (PayrollProcessingService)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 01 | BH tính trên baseSalary | base=10M, gross=15M | BHXH = 800K (8% × 10M) |
| 02 | BH có mức trần | base=100M, cap=36M | BHXH = 2.88M (8% × 36M) |
| 03 | baseSalary = 0 | base=0 | BHXH = 0 |

### 🔴 BUG-2: TotalDeductions = 0 (PayrollProcessingService)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 01 | Có deductions | BH=1.05M, Tax=500K | TotalDeductions = 1.55M |
| 02 | Có debt | Trên + Debt=200K | TotalDeductions = 1.75M |
| 03 | All = 0 | Không có deductions | TotalDeductions = 0 |

### 🔴 BUG-3: Leave không check balance (LeaveRequestService)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 01 | Đủ balance | Balance=10, Request=3 | ✅ Thành công |
| 02 | Không đủ | Balance=2, Request=5 | ❌ "Insufficient balance" |
| 03 | Balance = 0 | Balance=0, Request=1 | ❌ "Insufficient balance" |
| 04 | Vừa đủ | Balance=5, Request=5 | ✅ Thành công |

### 🔴 BUG-4: Sửa đơn đã Approved (LeaveRequestService)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 01 | Sửa Pending | Status=Pending | ✅ OK |
| 02 | Sửa Approved | Status=Approved | ❌ "Cannot modify" |
| 03 | Sửa Rejected | Status=Rejected | ❌ "Cannot modify" |

### 🔴 BUG-5: OT Team Summary (AttendanceService)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 01 | OT từ DailyLog | OT=[1.5, 2.0] | TotalOT = 3.5 |
| 02 | Không OT | OT=[0, 0] | TotalOT = 0 |
| 03 | WH > 8 nhưng OT=0 | WH=9, OT=0 | TotalOT = 0 |

### 🔴 BUG-6: Email trùng (RegisterCommandHandler)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 01 | Email unique | new@test.com | ✅ OK |
| 02 | Email trùng | existing@test.com | ❌ "Email exists" |
| 03 | Case khác | A@Test vs a@test | ❌ "Email exists" |

---

## 3️⃣ Module Tests

### 🔐 Auth (9 test cases)

| # | Test Case | Expected |
|---|-----------|----------|
| 01 | Login username đúng | JWT token |
| 02 | Login email đúng | JWT token |
| 03 | Login sai password | "Invalid credentials" |
| 04 | Login user bị khóa | "Account disabled" |
| 05 | Register username trùng | "Username exists" |
| 06 | JWT chứa đủ claims | UserId, EmployeeId, Roles |
| 07 | Assign role | User có thêm role |
| 08 | Change password (đúng) | Thành công |
| 09 | Change password (sai old) | Error |

### 👤 Employee (9 test cases)

| # | Test Case | Expected |
|---|-----------|----------|
| 01 | Tạo NV hợp lệ | Thành công + Event |
| 02 | Code trùng | "Duplicate code" |
| 03 | Tuổi < 18 | "Under age" |
| 04 | DepartmentId sai | "Dept not found" |
| 05 | Auto tạo User | User created |
| 06 | View — role Employee | BankDetails = null |
| 07 | View — role Admin | BankDetails visible |
| 08 | Xóa NV là Manager | "Cannot delete" |
| 09 | Paging | Đúng page/total |

### 📄 Contract (8 test cases)

| # | Test Case | Expected |
|---|-----------|----------|
| 01 | Tạo HĐ hợp lệ | Thành công + Event |
| 02 | Start > End | Validation error |
| 03 | BaseSalary = 0 | Validation error |
| 04 | HĐ cũ auto Expired | Status = Expired |
| 05 | Overlap | "Overlap" error |
| 06 | Init Leave Allocation | Allocations created |
| 07 | Terminate | Status = Terminated |
| 08 | Audit log | OldValues/NewValues đúng |

### ⏰ Attendance (8 test cases)

| # | Test Case | Expected |
|---|-----------|----------|
| 01 | Present | Status = "Present" |
| 02 | Late | LateMinutes > 0 |
| 03 | Overtime | OT > 0 |
| 04 | Ghost Log (no checkout) | Graceful handling |
| 05 | Break deduction | WH trừ break |
| 06 | Overnight shift | Tính đúng |
| 07 | Update existing month | DailyLog cập nhật |
| 08 | Recalculate totals | Totals đúng |

### 🌴 Leave (8 test cases)

| # | Test Case | Expected |
|---|-----------|----------|
| 01 | Tạo đơn | Status = Pending |
| 02 | Approve → trừ UsedDays | Balance giảm |
| 03 | Reject → không trừ | Balance không đổi |
| 04 | Cancel Pending | Status = Cancelled |
| 05 | Cancel Approved → hoàn | Balance tăng |
| 06 | Monthly accrual | AccruedDays tăng |
| 07 | Accrual idempotent | Chạy 2x → không x2 |
| 08 | Init from Contract | All LeaveType allocated |

### 💰 Payroll (11 test cases - CRITICAL)

| # | Test Case | Expected |
|---|-----------|----------|
| 01 | Tính lương đầy đủ | Net = Gross - BH - Tax - Debt |
| 02 | Prorated salary | Salary × (Actual/Standard) |
| 03 | Thuế bậc 1 | ≤ 5M → 5% |
| 04 | Thuế bậc 7 | > 80M → 35% phần vượt |
| 05 | Giảm trừ cá nhân | Taxable -= 11M |
| 06 | Giảm trừ phụ thuộc | Taxable -= deps × 4.4M |
| 07 | Taxable < 0 | Tax = 0 |
| 08 | Nợ carry-forward | Net < 0 → ghi nợ |
| 09 | OvertimePay | OT × rate × wage |
| 10 | Snapshot | Match NV hiện tại |
| 11 | Không HĐ Active | Error hoặc skip |

### 🏢 Organization (6 test cases)

| # | Test Case | Expected |
|---|-----------|----------|
| 01 | Tạo Department | Thành công |
| 02 | Tạo Dept con | ParentId đúng |
| 03 | Cycle A→B→C→A | Error |
| 04 | Self-reference | Error |
| 05 | Tree JSON | Đúng phân cấp |
| 06 | Cache invalidation | Cache cleared |

---

## 4️⃣ API Tests (Swagger/Postman)

| # | Test | Method | Path | Expected |
|---|------|--------|------|----------|
| 01 | Login OK | POST | /api/auth/login | 200 + token |
| 02 | No token | GET | /api/employees/{id} | 401 |
| 03 | Wrong role | GET | /api/payrolls | 403 |
| 04 | Admin create emp | POST | /api/employees | 200 |
| 05 | HR create contract | POST | /api/contracts | 200 |
| 06 | Check-in | POST | /api/attendance/check-in | 200 |
| 07 | Request leave | POST | /api/leaves | 200 |
| 08 | Approve leave | PUT | /api/leaves/{id}/review | 200 |
| 09 | Generate payroll | POST | /api/payrolls/generate | 200 |
| 10 | Validation fail | POST | /api/employees (empty) | 400 |

---

## 5️⃣ E2E Scenarios

### Scenario 1: Employee Lifecycle

1. Admin tạo Employee (EMP-001) → Auto tạo User
2. HR tạo Contract → Auto init Leave
3. EMP-001 login → Check-In/Out
4. HR process logs → EMP-001 xin phép → Manager duyệt
5. HR tính lương → Verify NetSalary, Balance, Attendance

### Scenario 2: Payroll Accuracy

1. Tạo NV BaseSalary = 15M, Contract Active
2. NV làm 20/22 ngày, nghỉ phép 2 ngày
3. Verify: ProRated, BHXH (8% × 15M), Tax (7 bậc), Net

### Scenario 3: Leave Integrity

1. Balance = 12 → Xin 3 (Approved) → Balance = 9
2. Xin 5 (Approved) → Balance = 4
3. Xin 5 → Rejected (4 < 5)
4. Cancel đơn #2 → Balance = 9
5. Accrual → Balance = 10

---

## 6️⃣ Execution Plan

### Phase 1: Bug Fix (tuần 1-2)

- [ ] BUG-1: Payroll BH (4 TCs)
- [ ] BUG-2: TotalDeductions (3 TCs)
- [ ] BUG-3: Leave balance (4 TCs)
- [ ] BUG-4: Leave update (3 TCs)
- [ ] BUG-5: OT Summary (3 TCs)
- [ ] BUG-6: Email duplicate (3 TCs)

### Phase 2: Module Tests (tuần 3-4)

- [ ] Auth (9 TCs)
- [ ] Employee (9 TCs)
- [ ] Contract (8 TCs)
- [ ] Attendance (8 TCs)
- [ ] Leave (8 TCs)
- [ ] Payroll (11 TCs)
- [ ] Organization (6 TCs)

### Phase 3: Integration + E2E (tuần 5)

- [ ] API Tests (10 TCs)
- [ ] 3 E2E Scenarios

---

## 7️⃣ Acceptance Criteria

| Tiêu chí | Yêu cầu |
|---------|---------|
| Bug Fix Tests | 100% PASS |
| Unit Tests (P0) | 100% PASS |
| Unit Tests (P1) | ≥ 90% PASS |
| Integration Tests | 100% PASS |
| E2E Scenarios | 100% PASS |
| Code coverage | ≥ 60% |
| No regression | Tất cả test cũ PASS |
