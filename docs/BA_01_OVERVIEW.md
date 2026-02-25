# TÀI LIỆU PHÂN TÍCH NGHIỆP VỤ (Business Analysis Document)
# HỆ THỐNG QUẢN LÝ NHÂN SỰ — EmployeeCleanArch HRM

| Thông tin | Chi tiết |
|-----------|----------|
| **Phiên bản** | 1.0 |
| **Ngày tạo** | 17/02/2026 |
| **Trạng thái** | Draft — Chờ duyệt |
| **Tác giả** | BA Team |

---

## MỤC LỤC

1. [Tổng quan dự án](#1-tổng-quan-dự-án)
2. [Phạm vi hệ thống](#2-phạm-vi-hệ-thống)
3. [Actors & Roles](#3-actors--roles)
4. [Kiến trúc hệ thống](#4-kiến-trúc-hệ-thống)
5. Module chi tiết → xem `BA_02_MODULES.md`
6. Data Dictionary → xem `BA_03_DATA_DICTIONARY.md`
7. API Specification → xem `BA_04_API_SPEC.md`
8. Business Rules → xem `BA_05_BUSINESS_RULES.md`
9. Gap Analysis & Roadmap → xem `BA_06_GAP_ANALYSIS.md`

---

## 1. TỔNG QUAN DỰ ÁN

### 1.1 Mục tiêu
Xây dựng **Hệ thống Quản lý Nhân sự (HRM)** toàn diện cho doanh nghiệp vừa và nhỏ tại Việt Nam, bao gồm:
- Quản lý hồ sơ nhân viên và hợp đồng lao động
- Chấm công và quản lý ca làm việc
- Quản lý nghỉ phép (cấp phát, xin phép, duyệt)
- Tính lương tự động theo quy định pháp luật Việt Nam
- Quản lý cơ cấu tổ chức (phòng ban, chức vụ)
- Phân quyền và bảo mật

### 1.2 Đối tượng sử dụng
Doanh nghiệp có từ 10–500 nhân viên, hoạt động tại Việt Nam, cần số hóa quy trình nhân sự.

### 1.3 Ngữ cảnh kinh doanh
Hệ thống thay thế các quy trình thủ công (Excel, giấy tờ) bằng phần mềm tự động hóa, giảm sai sót trong tính lương, chấm công, và quản lý nghỉ phép.

---

## 2. PHẠM VI HỆ THỐNG

### 2.1 Trong phạm vi (In Scope)

| # | Module | Mô tả | Trạng thái |
|---|--------|--------|-----------|
| M1 | **Authentication & Authorization** | Đăng nhập, phân quyền, quản lý user | ✅ Đã xây dựng |
| M2 | **Organization Management** | Phòng ban, chức vụ (phân cấp) | ✅ Đã xây dựng |
| M3 | **Human Resource** | Hồ sơ nhân viên, hợp đồng lao động | ✅ Đã xây dựng |
| M4 | **Attendance Management** | Chấm công, ca làm, xử lý log | ✅ Đã xây dựng |
| M5 | **Leave Management** | Loại phép, cấp phát, xin/duyệt phép | ✅ Đã xây dựng |
| M6 | **Payroll** | Tính lương, bảo hiểm, thuế TNCN | ✅ Đã xây dựng |
| M7 | **Recruitment** | Tin tuyển dụng, ứng viên, phỏng vấn | ⚠️ Chỉ có Entity |
| M8 | **Common Services** | Audit Log, Dashboard, File, Cache, Settings | ✅ Đã xây dựng |

### 2.2 Ngoài phạm vi (Out of Scope)
- Quản lý đào tạo (Training)
- Đánh giá KPI (Performance Review)
- Quản lý tài sản (Asset Management)
- Mobile App
- Tích hợp máy chấm công vật lý
- Báo cáo thuế gửi cơ quan nhà nước

---

## 3. ACTORS & ROLES

### 3.1 Ma trận phân quyền

| Chức năng | Admin | HR | Manager | Employee |
|-----------|:-----:|:--:|:-------:|:--------:|
| **Auth** — Login | ✅ | ✅ | ✅ | ✅ |
| **Auth** — Register User | ✅ | ❌ | ❌ | ❌ |
| **Auth** — Manage Roles | ✅ | ❌ | ❌ | ❌ |
| **Auth** — Enable/Disable User | ✅ | ✅ | ❌ | ❌ |
| **Auth** — Change Own Password | ✅ | ✅ | ✅ | ✅ |
| **Auth** — View User List | ✅ | ✅ | ❌ | ❌ |
| **HR** — Create/Edit Employee | ✅ | ✅ | ❌ | ❌ |
| **HR** — Delete Employee | ✅ | ❌ | ❌ | ❌ |
| **HR** — View Employee List | ✅ | ✅ | ✅ | ✅ |
| **HR** — View Employee Detail | ✅ | ✅ | ✅ | ✅ (BankDetails ẩn) |
| **HR** — View Org Chart | ✅ | ✅ | ✅ | ✅ |
| **Contract** — CRUD | ✅ | ✅ | ❌ | ❌ |
| **Attendance** — Check In/Out | ✅ | ✅ | ✅ | ✅ |
| **Attendance** — View Own | ✅ | ✅ | ✅ | ✅ |
| **Attendance** — View Team | ✅ | ✅ | ✅ | ❌ |
| **Attendance** — Process Logs | ✅ | ✅ | ❌ | ❌ |
| **Leave** — Create/Cancel Own | ✅ | ✅ | ✅ | ✅ |
| **Leave** — View Own Leaves | ✅ | ✅ | ✅ | ✅ |
| **Leave** — Review (Approve/Reject) | ✅ | ✅ | ✅ | ❌ |
| **Leave** — View All Requests | ✅ | ✅ | ✅ | ❌ |
| **Payroll** — View Own Salary | ✅ | ✅ | ✅ | ✅ |
| **Payroll** — Generate Payroll | ✅ | ✅ | ❌ | ❌ |
| **Payroll** — Finalize/Approve | ✅ | ✅ | ❌ | ❌ |
| **Payroll** — View All | ✅ | ✅ | ❌ | ❌ |
| **Organization** — Manage Dept/Position | ✅ | ✅ | ❌ | ❌ |

### 3.2 Mô tả Actors

**Admin**: Quản trị viên hệ thống, có toàn quyền. Quản lý user, role, cấu hình hệ thống.

**HR (Human Resources)**: Nhân viên phòng nhân sự. Quản lý hồ sơ, hợp đồng, tính lương, chấm công.

**Manager**: Quản lý/trưởng phòng. Duyệt đơn nghỉ phép, xem chấm công team.

**Employee**: Nhân viên thường. Chấm công, xin nghỉ, xem lương cá nhân.

---

## 4. KIẾN TRÚC HỆ THỐNG

### 4.1 Technology Stack

| Thành phần | Công nghệ | Phiên bản |
|-----------|-----------|-----------|
| **Backend Framework** | ASP.NET Core (Minimal API) | .NET 8+ |
| **API Framework** | Carter (Minimal API modules) | Latest |
| **CQRS/Mediator** | MediatR | Latest |
| **Validation** | FluentValidation | Latest |
| **Database** | MongoDB (NoSQL) | Latest |
| **Identity** | ASP.NET Identity (MongoDB adapter) | Latest |
| **Authentication** | JWT Bearer Token | — |
| **Caching** | Redis (StackExchange) | Alpine |
| **Frontend** | Angular + Tailwind CSS | — |
| **Containerization** | Docker + Docker Compose | — |

### 4.2 Kiến trúc phần mềm

```
┌──────────────────────────────────────────────┐
│              PRESENTATION LAYER               │
│     Employee.API (Carter Minimal API)         │
│   Endpoints ── Middlewares ── Swagger/JWT      │
├──────────────────────────────────────────────┤
│              APPLICATION LAYER                │
│      Employee.Application (MediatR CQRS)      │
│  Commands ── Queries ── Events ── Services     │
│  DTOs ── Mappers ── Validators (Fluent)        │
├──────────────────────────────────────────────┤
│               DOMAIN LAYER                    │
│           Employee.Domain                     │
│  Entities ── Value Objects ── Constants        │
├──────────────────────────────────────────────┤
│            INFRASTRUCTURE LAYER               │
│         Employee.Infrastructure               │
│  Repositories ── Services ── Persistence       │
│  Background Jobs ── MongoDB ── Redis           │
└──────────────────────────────────────────────┘
```

### 4.3 Deployment Architecture

```
┌─────────────────────────────────────┐
│          Docker Compose             │
│                                     │
│  ┌───────────┐  ┌────────────────┐  │
│  │  MongoDB   │  │  .NET API      │  │
│  │  :27017    │←─│  :5000→8080    │  │
│  └───────────┘  └───────┬────────┘  │
│                         │           │
│  ┌───────────┐          │           │
│  │  Redis     │←─────────┘           │
│  │  :6379     │                     │
│  └───────────┘                      │
└─────────────────────────────────────┘
         ↑
    Angular :4200
```
