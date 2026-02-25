# 🎯 Gap Analysis & Roadmap

---

## 🔴 CRITICAL — BUG Phải Fix Ngay

| # | Module | Vấn đề | Trạng thái |
|---|--------|--------|-----------|
| BUG-1 | 💰 Payroll | BHXH/BHYT/BHTN tính trên grossIncome | ✅ **FIXED** |
| BUG-2 | 💰 Payroll | TotalDeductions luôn = 0 | ✅ **FIXED** |
| BUG-3 | 🌴 Leave | Tạo đơn không check số dư | ✅ **FIXED** |
| BUG-4 | 🌴 Leave | Sửa đơn đã Approved/Rejected | ✅ **FIXED** |
| BUG-5 | ⏰ Attendance | OT Team Summary tính sai | ✅ **FIXED** |
| BUG-6 | 🔐 Auth | Register không check email trùng | ✅ **FIXED** |

---

## 🟡 HIGH — Thiếu Sót Quan Trọng

| # | Module | Vấn đề | Trạng thái |
|---|--------|--------|-----------|
| MISS-1 | 🔐 Auth | Login không check user bị khóa | ✅ **FIXED** |
| MISS-2 | 👤 HR | Password hardcoded | ✅ **FIXED** |
| MISS-3 | 🌴 Leave | Tạo đơn không check overlap | ✅ **FIXED** |
| MISS-4 | 🌴 Leave | Approve không ghi ApprovedBy | ✅ **DONE** (Partial) |
| MISS-5 | 🌴 Leave | Accrual filter Status không chuẩn | ✅ **FIXED** |
| MISS-6 | ⚙️ Code | Background Service đăng ký trùng | ✅ **FIXED** |

---

## 🟠 MEDIUM — Cải Thiện

| # | Module | Vấn đề | Trạng thái |
|---|--------|--------|-----------|
| IMP-1 | 🏢 Org | Delete Dept/Position không check references | ✅ **FIXED** |
| IMP-2 | 🏢 Org | DepartmentTree hiển thị ManagerId | ✅ **FIXED** |
| IMP-3 | 👤 HR | Delete Employee không cleanup | ✅ **FIXED** |
| IMP-4 | 🌴 Leave | Cancel đơn Approved không hoàn phép | ✅ **FIXED** |
| IMP-5 | ⏰ Attendance | Team Summary lấy ALL employees | ✅ **FIXED** |
| IMP-6 | 💰 Payroll | Bonus field chưa bao giờ được gán | ⚠️ Draft |

---

## 🔵 LOW — Tính Năng Mới

| # | Mô tả | Priority |
|---|-------|---------|
| NEW-1 | Hoàn thiện module Recruitment (Services, APIs, Workflow) | ✅ **DONE** |
| NEW-2 | Refresh Token mechanism | ✅ **DONE** |
| NEW-3 | Forgot/Reset Password | ✅ **DONE** |
| NEW-4 | Implement Sandwich Rule logic | P3 |
| NEW-5 | Carry Forward year-end automation | P3 |
| NEW-6 | Xuất phiếu lương PDF | P3 |
| NEW-7 | Báo cáo tổng hợp thuế TNCN năm | P3 |
| NEW-8 | Dashboard thống kê tổng quan | ✅ **DONE** |
| NEW-9 | Notification system (email/in-app) | P3 |
| NEW-10 | Export Excel cho các danh sách | P3 |

---

## 📅 ROADMAP — Lộ Trình Phát Triển

### Phase 1: Bug Fix & Security (1–2 tuần)

Mục tiêu: Fix tất cả BUG nghiêm trọng, đảm bảo data integrity.

- [x] BUG-1: Fix BHXH/BHYT/BHTN tính trên baseSalary
- [x] BUG-2: Fix TotalDeductions = 0
- [x] BUG-3: Leave — check balance trước khi tạo
- [x] BUG-4: Leave — block sửa đơn đã Approved/Rejected
- [x] BUG-5: Fix OT trong Team Summary
- [x] BUG-6: Auth — check email trùng
- [x] MISS-1: Auth — check user disabled khi Login
- [x] MISS-2: HR — bỏ hardcoded password
- [x] MISS-6: Fix DI đăng ký trùng

### Phase 2: Business Logic Enhancement (2–3 tuần)

Mục tiêu: Hoàn thiện nghiệp vụ, tăng tính chính xác.

- [x] MISS-3: Leave — overlap check
- [x] MISS-4: Leave — ghi ApprovedBy
- [x] MISS-5: Leave — fix accrual filter
- [x] IMP-1: Organization — delete reference check
- [x] IMP-2: Organization — hiển thị tên Manager
- [x] IMP-3: HR — employee deletion cleanup handler
- [x] IMP-4: Leave — refund days khi cancel Approved
- [x] IMP-5: Attendance — filter team theo manager

### Phase 3: New Features (3–4 tuần)

Mục tiêu: Mở rộng tính năng, tăng trải nghiệm.

- [x] NEW-1: Hoàn thiện module Recruitment
- [x] NEW-2: Refresh Token
- [x] NEW-3: Forgot/Reset Password
- [ ] NEW-6: Export phiếu lương PDF
- [ ] NEW-10: Export Excel
- [x] NEW-8: Dashboard thống kê

### Phase 4: Advanced Features (4+ tuần)

Mục tiêu: Tính năng nâng cao.

- [ ] NEW-4: Sandwich Rule
- [ ] NEW-5: Carry Forward automation
- [ ] NEW-7: Báo cáo thuế TNCN năm
- [ ] NEW-9: Notification system

---

## 📊 Non-Functional Requirements

### Bảo mật

| Yêu cầu | Status |
|---------|--------|
| JWT Authentication | ✅ |
| Role-based Authorization | ✅ |
| Password Hashing (Identity) | ✅ |
| CORS Configuration | ✅ |
| BankDetails Access Control | ✅ |
| Input Validation (FluentValidation) | ✅ |
| Rate Limiting | ❌ |
| Data Encryption at Rest | ❌ |

### Khả năng bảo trì

| Yêu cầu | Status |
|---------|--------|
| Clean Architecture | ✅ |
| CQRS Pattern (MediatR) | ✅ |
| Unit of Work Pattern | ✅ |
| Event-Driven Architecture | ✅ |
| Soft Delete | ✅ |
| Optimistic Concurrency | ✅ |
| Unit Tests | ❌ |
| Integration Tests | ❌ |

---

## 📈 Thống Kê Tổng Quan

| Metric | Số lượng |
|--------|---------|
| Tổng modules | 8 (7 hoàn thiện + 1 chưa) |
| Tổng entities | 15+ |
| Tổng API endpoints | 40+ |
| Business rules đã implement | ~35 |
| Business rules chưa implement | ~15 |
| BUG cần fix | 6 |
| Thiếu sót cần bổ sung | 6 |
| Cải thiện cần làm | 6 |
| Tính năng mới | 10 |
