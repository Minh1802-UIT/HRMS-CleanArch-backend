# 📋 HRM EmployeeCleanArch — Tổng Quan Dự Án

## 📄 Thông tin tài liệu

| Thông tin | Chi tiết |
|-----------|----------|
| Phiên bản | 1.0 |
| Ngày tạo | 17/02/2026 |
| Trạng thái | Draft — Chờ duyệt |

---

## 1️⃣ Mục tiêu

Xây dựng **Hệ thống Quản lý Nhân sự (HRM)** toàn diện cho doanh nghiệp vừa và nhỏ tại Việt Nam:

- 👤 Quản lý hồ sơ nhân viên và hợp đồng lao động
- ⏰ Chấm công và quản lý ca làm việc
- 🌴 Quản lý nghỉ phép (cấp phát, xin phép, duyệt)
- 💰 Tính lương tự động theo quy định pháp luật Việt Nam
- 🏢 Quản lý cơ cấu tổ chức (phòng ban, chức vụ)
- 🔐 Phân quyền và bảo mật

**Đối tượng**: Doanh nghiệp 10–500 nhân viên, hoạt động tại Việt Nam.

---

## 2️⃣ Phạm vi hệ thống

### ✅ Trong phạm vi (In Scope)

| # | Module | Mô tả | Trạng thái |
|---|--------|--------|-----------|
| M1 | 🔐 Authentication | Đăng nhập, phân quyền, quản lý user | ✅ Đã xây dựng |
| M2 | 🏢 Organization | Phòng ban, chức vụ (phân cấp) | ✅ Đã xây dựng |
| M3 | 👤 Human Resource | Hồ sơ nhân viên, hợp đồng lao động | ✅ Đã xây dựng |
| M4 | ⏰ Attendance | Chấm công, ca làm, xử lý log | ✅ Đã xây dựng |
| M5 | 🌴 Leave Management | Loại phép, cấp phát, xin/duyệt phép | ✅ Đã xây dựng |
| M6 | 💰 Payroll | Tính lương, bảo hiểm, thuế TNCN | ✅ Đã xây dựng |
| M7 | 📝 Recruitment | Tin tuyển dụng, ứng viên, phỏng vấn | ⚠️ Chỉ có Entity |
| M8 | ⚙️ Common Services | Audit Log, Dashboard, File, Cache, Settings | ✅ Đã xây dựng |

### ❌ Ngoài phạm vi

- Quản lý đào tạo (Training)
- Đánh giá KPI (Performance Review)
- Quản lý tài sản (Asset Management)
- Mobile App
- Tích hợp máy chấm công vật lý
- Báo cáo thuế gửi cơ quan nhà nước

---

## 3️⃣ Actors và Phân Quyền

### Mô tả Actors

- **🔴 Admin**: Quản trị viên hệ thống, có toàn quyền. Quản lý user, role, cấu hình hệ thống.
- **🟠 HR**: Nhân viên phòng nhân sự. Quản lý hồ sơ, hợp đồng, tính lương, chấm công.
- **🟡 Manager**: Quản lý/trưởng phòng. Duyệt đơn nghỉ phép, xem chấm công team.
- **🟢 Employee**: Nhân viên thường. Chấm công, xin nghỉ, xem lương cá nhân.

### Ma trận phân quyền — Authentication

| Chức năng | Admin | HR | Manager | Employee |
|-----------|:-----:|:--:|:-------:|:--------:|
| Login | ✅ | ✅ | ✅ | ✅ |
| Register User | ✅ | ❌ | ❌ | ❌ |
| Manage Roles | ✅ | ❌ | ❌ | ❌ |
| Enable/Disable User | ✅ | ✅ | ❌ | ❌ |
| Change Own Password | ✅ | ✅ | ✅ | ✅ |
| View User List | ✅ | ✅ | ❌ | ❌ |

### Ma trận phân quyền — Human Resource

| Chức năng | Admin | HR | Manager | Employee |
|-----------|:-----:|:--:|:-------:|:--------:|
| Create/Edit Employee | ✅ | ✅ | ❌ | ❌ |
| Delete Employee | ✅ | ❌ | ❌ | ❌ |
| View Employee List | ✅ | ✅ | ✅ | ✅ |
| View Employee Detail | ✅ | ✅ | ✅ | ✅ (BankDetails ẩn) |
| View Org Chart | ✅ | ✅ | ✅ | ✅ |
| Contract CRUD | ✅ | ✅ | ❌ | ❌ |

### Ma trận phân quyền — Attendance & Leave

| Chức năng | Admin | HR | Manager | Employee |
|-----------|:-----:|:--:|:-------:|:--------:|
| Check In/Out | ✅ | ✅ | ✅ | ✅ |
| View Own Attendance | ✅ | ✅ | ✅ | ✅ |
| View Team Attendance | ✅ | ✅ | ✅ | ❌ |
| Process Logs | ✅ | ✅ | ❌ | ❌ |
| Create/Cancel Leave | ✅ | ✅ | ✅ | ✅ |
| View Own Leaves | ✅ | ✅ | ✅ | ✅ |
| Review Leave | ✅ | ✅ | ✅ | ❌ |
| View All Leaves | ✅ | ✅ | ✅ | ❌ |

### Ma trận phân quyền — Payroll & Organization

| Chức năng | Admin | HR | Manager | Employee |
|-----------|:-----:|:--:|:-------:|:--------:|
| View Own Salary | ✅ | ✅ | ✅ | ✅ |
| Generate Payroll | ✅ | ✅ | ❌ | ❌ |
| Finalize/Approve Pay | ✅ | ✅ | ❌ | ❌ |
| View All Payroll | ✅ | ✅ | ❌ | ❌ |
| Manage Dept/Position | ✅ | ✅ | ❌ | ❌ |

---

## 4️⃣ Kiến trúc hệ thống

### Technology Stack

| Thành phần | Công nghệ |
|-----------|-----------|
| Backend Framework | ASP.NET Core (Minimal API) .NET 8+ |
| API Framework | Carter (Minimal API modules) |
| CQRS/Mediator | MediatR |
| Validation | FluentValidation |
| Database | MongoDB (NoSQL) |
| Identity | ASP.NET Identity (MongoDB adapter) |
| Authentication | JWT Bearer Token |
| Caching | Redis (StackExchange) |
| Frontend | Angular + Tailwind CSS |
| Containerization | Docker + Docker Compose |

### Kiến trúc phần mềm (4 Layers)

**Layer 1 — PRESENTATION** (Employee.API)
- Carter Minimal API Endpoints
- Middlewares, Swagger, JWT Auth

**Layer 2 — APPLICATION** (Employee.Application)
- MediatR CQRS: Commands, Queries, Events
- Services, DTOs, Mappers, Validators (FluentValidation)

**Layer 3 — DOMAIN** (Employee.Domain)
- Entities, Value Objects, Constants

**Layer 4 — INFRASTRUCTURE** (Employee.Infrastructure)
- Repositories, Services, Persistence
- Background Jobs, MongoDB, Redis

### Deployment

| Service | Port | Mô tả |
|---------|------|--------|
| MongoDB | 27017 | Database chính |
| .NET API | 5000 → 8080 | Backend REST API |
| Redis | 6379 | Cache |
| Angular | 4200 | Frontend |

Tất cả chạy qua **Docker Compose**.
