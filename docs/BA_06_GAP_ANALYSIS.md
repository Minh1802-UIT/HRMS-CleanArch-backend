# GAP ANALYSIS & ROADMAP

> Phân tích khoảng trống, danh sách lỗi cần fix, và lộ trình phát triển.

---

## 1. PHÂN LOẠI VẤN ĐỀ

### 🔴 CRITICAL — BUG Logic Nghiệp Vụ (Phải Fix Ngay)

| # | Module | Vấn đề | File | Trạng thái |
|---|--------|--------|------|-----------|
| BUG-1 | Payroll | BHXH/BHYT/BHTN tính trên `grossIncome` | PayrollProcessingService.cs | ✅ **FIXED** |
| BUG-2 | Payroll | `TotalDeductions` luôn = 0 | PayrollProcessingService.cs | ✅ **FIXED** |
| BUG-3 | Leave | Tạo đơn không check số dư phép | CreateLeaveRequestHandler.cs | ✅ **FIXED** |
| BUG-4 | Leave | Update cho sửa đơn đã Approved/Rejected | LeaveRequest.cs (Domain) | ✅ **FIXED** |
| BUG-5 | Attendance | OT trong Team Summary tính sai | AttendanceService.cs | ✅ **FIXED** |
| BUG-6 | Auth | Register không check email trùng | RegisterCommandHandler.cs | ✅ **FIXED** |

---

### 🟡 HIGH — Thiếu Sót Nghiệp Vụ Quan Trọng

| # | Module | Vấn đề | Trạng thái |
|---|--------|--------|-----------|
| MISS-1 | Auth | Login không check user bị khóa | ✅ **FIXED** |
| MISS-2 | HR | Password mặc định hardcoded | ✅ **FIXED** |
| MISS-3 | Leave | Tạo đơn không check overlap | ✅ **FIXED** |
| MISS-4 | Leave | Approve không ghi `ApprovedBy` | ✅ **DONE** (Partial) |
| MISS-5 | Leave | Accrual filter `Status` chưa chuẩn | ✅ **FIXED** |
| MISS-6 | CodeQuality | `LeaveAccrualBackgroundService` đăng ký 2 lần | ✅ **FIXED** |

---

### 🟠 MEDIUM — Cải Thiện Nghiệp Vụ

| # | Module | Vấn đề | Giải pháp |
|---|--------|--------|-----------|
| IMP-1 | Organization | Delete Department/Position không check references | Check employee/child trước khi xóa |
| IMP-2 | Organization | DepartmentTree hiển thị ManagerId thay vì tên | Join với Employee để lấy FullName |
| IMP-3 | HR | Delete Employee không cleanup data liên quan | Implement EmployeeDeletedEvent handler |
| IMP-4 | Leave | Cancel đơn Approved → không hoàn phép | Gọi RefundDaysAsync |
| IMP-5 | Attendance | Team Summary lấy ALL employees, không filter theo managerId | Filter theo manager's subordinates |
| IMP-6 | Payroll | Trường `Bonus` trong Entity nhưng không bao giờ được gán | Implement bonus logic hoặc bỏ field |

---

### 🔵 LOW — Tính Năng Mở Rộng

| # | Module | Mô tả | Priority |
|---|--------|-------|---------|
| NEW-1 | Recruitment | Hoàn thiện module: Services, DTOs, APIs, Workflow | P2 |
| NEW-2 | Auth | Refresh Token mechanism | P2 |
| NEW-3 | Auth | Forgot/Reset Password | P2 |
| NEW-4 | Leave | Implement Sandwich Rule logic | P3 |
| NEW-5 | Leave | Carry Forward year-end automation | P3 |
| NEW-6 | Payroll | Xuất phiếu lương PDF | P3 |
| NEW-7 | Payroll | Báo cáo tổng hợp thuế TNCN năm | P3 |
| NEW-8 | Common | Dashboard thống kê tổng quan | P3 |
| NEW-9 | Common | Notification (email/in-app) | P3 |
| NEW-10 | Common | Export Excel cho các danh sách | P3 |

---

## 2. ROADMAP — LỘ TRÌNH PHÁT TRIỂN

### Phase 1: Bug Fix & Security (1–2 tuần)
**Mục tiêu**: Fix tất cả BUG nghiêm trọng, đảm bảo data integrity.

- [x] BUG-1: Fix BHXH/BHYT/BHTN tính trên baseSalary
- [x] BUG-2: Fix TotalDeductions = 0
- [x] BUG-3: Leave — check balance trước khi tạo đơn
- [x] BUG-4: Leave — block sửa đơn đã Approved/Rejected
- [x] BUG-5: Fix OT calculation trong Team Summary
- [x] BUG-6: Auth — check email trùng khi Register
- [x] MISS-1: Auth — check user disabled khi Login
- [x] MISS-2: HR — bỏ hardcoded password
- [x] MISS-6: Fix DI đăng ký trùng BackgroundService

### Phase 2: Business Logic Enhancement (2–3 tuần)
**Mục tiêu**: Hoàn thiện nghiệp vụ, tăng tính chính xác.

- [x] MISS-3: Leave — overlap check
- [x] MISS-4: Leave — ghi ApprovedBy
- [x] MISS-5: Leave — fix accrual filter
- [x] IMP-1: Organization — delete reference check
- [x] IMP-2: Organization — hiển thị tên Manager
- [x] IMP-3: HR — employee deletion cleanup handler
- [x] IMP-4: Leave — refund days khi cancel Approved
- [x] IMP-5: Attendance — filter team theo manager

### Phase 3: New Features (3–4 tuần)
**Mục tiêu**: Mở rộng tính năng, tăng trải nghiệm.

- [ ] NEW-1: Hoàn thiện module Recruitment
- [ ] NEW-2: Refresh Token
- [ ] NEW-3: Forgot/Reset Password
- [ ] NEW-6: Export phiếu lương PDF
- [ ] NEW-10: Export Excel
- [ ] NEW-8: Dashboard thống kê

### Phase 4: Advanced Features (4+ tuần)
**Mục tiêu**: Tính năng nâng cao.

- [ ] NEW-4: Sandwich Rule
- [ ] NEW-5: Carry Forward automation
- [ ] NEW-7: Báo cáo thuế TNCN năm
- [ ] NEW-9: Notification system
- [ ] Training module
- [ ] KPI/Performance Review module

---

## 3. NON-FUNCTIONAL REQUIREMENTS

### 3.1 Hiệu năng
| Yêu cầu | Mục tiêu | Hiện tại |
|---------|----------|---------|
| Response time API | < 500ms | ✅ Đạt (ước tính) |
| Concurrent users | 50+ | ⚠️ Chưa test |
| Database queries | Indexed | ⚠️ Cần review indexes |

### 3.2 Bảo mật
| Yêu cầu | Trạng thái |
|---------|-----------|
| JWT Authentication | ✅ |
| Role-based Authorization | ✅ |
| Password Hashing (Identity) | ✅ |
| CORS Configuration | ✅ |
| BankDetails Access Control | ✅ |
| Input Validation (FluentValidation) | ✅ |
| Rate Limiting | ❌ Chưa implement |
| Audit Logging | ✅ (Contract module) |
| Data Encryption at Rest | ❌ |

### 3.3 Khả năng mở rộng
| Yêu cầu | Trạng thái |
|---------|-----------|
| Docker containerization | ✅ |
| Horizontal scaling | ⚠️ Cần session/cache sync |
| MongoDB Replica Set | ❌ Single node |

### 3.4 Khả năng bảo trì
| Yêu cầu | Trạng thái |
|---------|-----------|
| Clean Architecture | ✅ |
| CQRS Pattern (MediatR) | ✅ |
| Unit of Work Pattern | ✅ |
| Event-Driven Architecture | ✅ |
| Soft Delete | ✅ |
| Optimistic Concurrency (Version) | ✅ |
| Unit Tests | ❌ Thiếu |
| Integration Tests | ❌ Thiếu |

---

## 4. TÓM TẮT THỐNG KÊ

| Metric | Số lượng |
|--------|---------|
| **Tổng modules** | 8 (7 hoàn thiện + 1 chưa hoàn thiện) |
| **Tổng entities** | 15+ (gồm Value Objects) |
| **Tổng API endpoints** | 40+ |
| **Business rules đã implement** | ~35 |
| **Business rules chưa implement** | ~15 |
| **BUG cần fix** | 6 |
| **Thiếu sót cần bổ sung** | 6 |
| **Cải thiện cần làm** | 6 |
| **Tính năng mới** | 10 |

---

> **Kết luận**: Hệ thống có nền tảng kiến trúc tốt (Clean Architecture, CQRS, Event-Driven). Cần ưu tiên fix 6 BUG nghiệp vụ nghiêm trọng (đặc biệt Payroll và Leave) trước khi đưa vào production. Sau đó hoàn thiện business rules còn thiếu và mở rộng tính năng theo roadmap.
