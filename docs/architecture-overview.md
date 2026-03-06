# Architecture Overview — HRMS (Human Resource Management System)

> **Document Version:** 1.0  
> **Last Updated:** March 6, 2026  
> **Author:** Senior Developer  
> **Status:** Living Document

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [High-Level Architecture](#2-high-level-architecture)
3. [Backend — Clean Architecture](#3-backend--clean-architecture)
   - 3.1 [Layer: Domain](#31-layer-domain)
   - 3.2 [Layer: Application](#32-layer-application)
   - 3.3 [Layer: Infrastructure](#33-layer-infrastructure)
   - 3.4 [Layer: API (Presentation)](#34-layer-api-presentation)
4. [Frontend — Angular SPA](#4-frontend--angular-spa)
5. [Data Architecture](#5-data-architecture)
6. [Authentication & Authorization](#6-authentication--authorization)
7. [Background Jobs & Event System](#7-background-jobs--event-system)
8. [Caching Strategy](#8-caching-strategy)
9. [Cross-Cutting Concerns](#9-cross-cutting-concerns)
10. [Infrastructure & Deployment](#10-infrastructure--deployment)
11. [Testing Strategy](#11-testing-strategy)
12. [Dependency Map](#12-dependency-map)

---

## 1. System Overview

HRMS là hệ thống quản lý nhân sự toàn diện được xây dựng theo kiến trúc **Full-Stack Monorepo** gồm hai phần:

| Thành phần | Công nghệ | Mục đích |
|---|---|---|
| **Backend API** | ASP.NET Core 8 (.NET 8) | REST API, business logic, data persistence |
| **Frontend SPA** | Angular 17 | Giao diện người dùng |

### Phạm vi nghiệp vụ

Hệ thống bao gồm **10 module nghiệp vụ** chính:

| Module | Mô tả |
|---|---|
| **Authentication** | Đăng nhập, phân quyền, quản lý token |
| **Human Resource** | Quản lý nhân viên, hợp đồng |
| **Organization** | Phòng ban, chức vụ, sơ đồ tổ chức |
| **Recruitment** | Tin tuyển dụng, ứng viên, phỏng vấn |
| **Attendance** | Chấm công, ca làm việc, lịch sử chấm công |
| **Leave Management** | Loại nghỉ phép, phân bổ phép, xét duyệt đơn |
| **Payroll** | Bảng lương, chu kỳ lương, báo cáo thuế PIT |
| **Performance** | Đánh giá hiệu suất, mục tiêu cá nhân |
| **Notifications** | Thông báo nội bộ real-time |
| **System** | Cài đặt hệ thống, quản lý user, audit log |

---

## 2. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT TIER                              │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │            Angular 17 SPA (HRMS-UI)                     │   │
│   │   PrimeNG · TailwindCSS · Chart.js · Lazy-loaded Routes │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │ HTTPS / REST                          │
└─────────────────────────┼───────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────┐
│                        API TIER                                 │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │          ASP.NET Core 8 Web API                         │   │
│   │   Carter (Minimal API) · JWT Bearer · Rate Limiting     │   │
│   │   Serilog · Swagger · API Versioning (v1)               │   │
│   └──────────────────────┬──────────────────────────────────┘   │
│                          │                                      │
│   ┌──────────────────────▼──────────────────────────────────┐   │
│   │          Application Layer (MediatR CQRS)               │   │
│   │   Commands · Queries · Validators · Pipeline Behaviors  │   │
│   └──────────────────────┬──────────────────────────────────┘   │
│                          │                                      │
│   ┌──────────────────────▼──────────────────────────────────┐   │
│   │              Domain Layer (Core)                        │   │
│   │   Entities · Value Objects · Domain Events · Interfaces │   │
│   └──────────────────────┬──────────────────────────────────┘   │
│                          │                                      │
│   ┌──────────────────────▼──────────────────────────────────┐   │
│   │           Infrastructure Layer                          │   │
│   │   MongoDB · Redis · Hangfire · Email · File Storage     │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────┐
│                       DATA TIER                                 │
│                                                                 │
│   ┌────────────────┐   ┌───────────────┐   ┌──────────────┐    │
│   │   MongoDB 7    │   │  Redis Cache  │   │  File Store  │    │
│   │  (Primary DB)  │   │  (L2 Cache)   │   │  (Supabase / │    │
│   │                │   │               │   │  LocalDisk)  │    │
│   └────────────────┘   └───────────────┘   └──────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Backend — Clean Architecture

Backend tuân theo **Clean Architecture** (Robert C. Martin), đảm bảo nguyên tắc **Dependency Inversion**: các layer bên trong không phụ thuộc vào layer bên ngoài.

```
┌──────────────────────────────────────────────┐
│              Employee.API                    │  ← Presentation Layer
│   (Carter, Endpoints, Middlewares, DI)       │
├──────────────────────────────────────────────┤
│           Employee.Application               │  ← Application Layer
│   (MediatR, CQRS, FluentValidation)          │
├──────────────────────────────────────────────┤
│             Employee.Domain                  │  ← Domain Layer (Core)
│   (Entities, ValueObjects, Interfaces)       │
├──────────────────────────────────────────────┤
│          Employee.Infrastructure             │  ← Infrastructure Layer
│   (MongoDB, Redis, Hangfire, Email)          │
└──────────────────────────────────────────────┘
```

### Dependency Rule

```
API  ──depends on──►  Application  ──depends on──►  Domain
                      Infrastructure ──depends on──►  Domain
                      Infrastructure ──implements──►  Application interfaces
```

Domain không phụ thuộc vào bất kỳ project nào. Infrastructure không được phép import trực tiếp vào Domain.

---

### 3.1 Layer: Domain

**Project:** `Employee.Domain`  
**NuGet dependency:** Chỉ `MongoDB.Bson` (cho attribute mapping).

Đây là **trái tim của hệ thống** — chứa toàn bộ business rules và không chứa bất kỳ dependency framework nào.

#### 3.1.1 Entities

Tất cả entities kế thừa từ `BaseEntity`, cung cấp:

| Property | Type | Mô tả |
|---|---|---|
| `Id` | `string` | MongoDB ObjectId (string representation) |
| `IsDeleted` | `bool` | Soft delete flag |
| `CreatedAt` | `DateTime` | Thời điểm tạo |
| `CreatedBy` | `string` | User tạo record |
| `UpdatedAt` | `DateTime?` | Thời điểm cập nhật cuối |
| `UpdatedBy` | `string?` | User cập nhật cuối |
| `Version` | `int` | Optimistic concurrency version |
| `DomainEvents` | `IReadOnlyCollection<IDomainEvent>` | Domain events đã raise |

#### 3.1.2 Domain Entities theo Module

**Organization:**
- `Department` — Phòng ban
- `Position` — Chức vụ

**Human Resource:**
- `EmployeeEntity` — Nhân viên (chứa embedded `PersonalInfo`, `JobDetails`, `BankDetails`)
- `Contract` — Hợp đồng lao động
- `JobVacancy` — Tin tuyển dụng
- `Candidate` — Ứng viên
- `Interview` — Lịch phỏng vấn

**Attendance:**
- `AttendanceBucket` — Bucket chấm công theo tháng (bucket pattern: mỗi employee/tháng = 1 document, chứa list `DailyLog`)
- `RawAttendanceLog` — Log check-in/check-out thô
- `Shift` — Ca làm việc

**Leave Management:**
- `LeaveType` — Loại nghỉ phép
- `LeaveAllocation` — Phân bổ ngày phép cho nhân viên
- `LeaveRequest` — Đơn xin nghỉ

**Payroll:**
- `PayrollEntity` — Bảng lương tháng (snapshot đầy đủ: income, deductions, PIT, net salary)
- `PayrollCycle` — Chu kỳ lương
- `PublicHoliday` — Ngày lễ quốc gia

**Performance:**
- `PerformanceReview` — Đánh giá hiệu suất
- `PerformanceGoal` — Mục tiêu cá nhân

**Notifications:**
- `Notification` — Thông báo nội bộ

#### 3.1.3 Value Objects

Các Value Objects được embed trực tiếp vào entity (không phải foreign key):

| Value Object | Được dùng trong |
|---|---|
| `PersonalInfo` | `EmployeeEntity` (Dob, Phone, Gender, Address, ...) |
| `JobDetails` | `EmployeeEntity` (DepartmentId, PositionId, BaseSalary, StartDate, ...) |
| `BankDetails` | `EmployeeEntity` (BankName, AccountNumber, ...) |
| `SalaryComponents` | `Contract` (BasicSalary, TransportAllowance, LunchAllowance, OtherAllowance) |
| `SalaryRange` | `Position` (Min, Max, Currency — dùng để hiển thị range lương của chức vụ) |
| `DailyLog` | `AttendanceBucket` (Date, CheckIn, CheckOut, Status, OvertimeHours) |
| `EmployeeSnapshot` | `PayrollEntity` (FullName, Email, Department tại thời điểm tính lương) |

#### 3.1.4 Domain Events

Domain Events theo pattern **Event-Driven** trong nội bộ Domain:

| Event | Trigger |
|---|---|
| `EmployeeCreatedEvent` | Tạo nhân viên mới |
| `EmployeeUpdatedEvent` | Cập nhật thông tin nhân viên |
| `EmployeeDeletedEvent` | Xóa nhân viên |
| `ContractCreatedEvent` | Ký hợp đồng mới |
| `LeaveRequestSubmittedEvent` | Nộp đơn xin nghỉ |
| `LeaveRequestApprovedEvent` | Duyệt đơn nghỉ |
| `LeaveRequestRejectedEvent` | Từ chối đơn nghỉ |

#### 3.1.5 Enums

| Enum | Giá trị |
|---|---|
| `EmployeeStatus` | Active, Inactive, Terminated, OnLeave |
| `ContractStatus` | Active, Expired, Terminated |
| `LeaveStatus` | Pending, Approved, Rejected, Cancelled |
| `LeaveCategory` | Annual, Sick, Unpaid, Maternity, ... |
| `AttendanceStatus` | Present, Absent, Late, HalfDay, Leave, Holiday |
| `PayrollStatus` | Draft, Calculated, Approved, Paid |
| `PayrollCycleStatus` | Open, Closed |
| `PerformanceReviewStatus` | Draft, Submitted, Completed |
| `PerformanceGoalStatus` | InProgress, Completed, Cancelled |
| `JobVacancyStatus` | Open, Closed, OnHold |
| `CandidateStatus` | Applied, Screening, Interview, Offered, Hired, Rejected |
| `InterviewStatus` | Scheduled, Completed, Cancelled |
| `RawLogType` | CheckIn, CheckOut |

#### 3.1.6 Repository Interfaces

Domain định nghĩa **Repository Contracts** — Infrastructure phải implement:

```
IBaseRepository<T>
├── IEmployeeRepository
├── IContractRepository
├── IDepartmentRepository
├── IPositionRepository
├── IAttendanceRepository
├── IRawAttendanceLogRepository
├── IShiftRepository
├── ILeaveTypeRepository
├── ILeaveAllocationRepository
├── ILeaveRequestRepository
├── IPayrollRepository
├── IPayrollCycleRepository
├── IPublicHolidayRepository
├── IPerformanceReviewRepository
├── IPerformanceGoalRepository
├── ICandidateRepository
├── IInterviewRepository
├── IJobVacancyRepository
├── INotificationRepository
├── IAuditLogRepository
└── ISystemSettingRepository
```

Ngoài ra còn có các **Query-specific interfaces** trong Application layer (tách biệt read/write):
- `IEmployeeQueryRepository` — truy vấn phức tạp cho employee (implement bởi `EmployeeRepository`)
- `IContractQueryRepository` — truy vấn phức tạp cho contract (implement bởi `ContractRepository`)

#### 3.1.7 Domain Services

Logic nghiệp vụ thuần túy không phụ thuộc vào infrastructure:

| Service | Mô tả |
|---|---|
| `ITaxCalculator` / `VietnameseTaxCalculator` | Tính thuế TNCN (PIT) theo biểu thuế lũy tiến của Việt Nam — 5 bậc. Đặt trong Domain để business rule không bị ô nhiễm bởi framework. |

---

### 3.2 Layer: Application

**Project:** `Employee.Application`

Layer này chứa toàn bộ **use-case logic** theo mô hình **CQRS + MediatR**.

#### 3.2.1 CQRS Pattern

Mọi request đều đi qua MediatR handler:

```
HTTP Request
    │
    ▼
Endpoint (Carter)
    │
    ▼  mediator.Send(command/query)
MediatR Pipeline
    │
    ├─── LoggingBehavior       (ghi log trước/sau handler)
    ├─── AuthorizationBehavior (kiểm tra permission)
    └─── ValidationBehavior    (FluentValidation)
    │
    ▼
Handler (Command/Query Handler)
    │
    ▼
Repository / Service
    │
    ▼
Result<T> / ApiResponse<T>
```

**Command** — thay đổi state:
```
Features/{Module}/Commands/{Feature}/
├── {Feature}Command.cs        (Request object)
├── {Feature}CommandHandler.cs (Business logic)
└── {Feature}CommandValidator.cs (FluentValidation rules)
```

**Query** — đọc data:
```
Features/{Module}/Queries/{Feature}/
├── {Feature}Query.cs
├── {Feature}QueryHandler.cs
└── {Feature}QueryValidator.cs (nếu có)
```

#### 3.2.2 Pipeline Behaviors (Middleware của MediatR)

| Behavior | Thứ tự | Nhiệm vụ |
|---|---|---|
| `LoggingBehavior` | 1 (outermost) | Log request name, execution time, CorrelationId |
| `AuthorizationBehavior` | 2 | Kiểm tra attribute `[Authorize]` trên Command/Query |
| `ValidationBehavior` | 3 (innermost) | Chạy FluentValidation, trả `ValidationException` nếu fail |

#### 3.2.3 Application Services

Các business service phức tạp được tách thành service riêng:

| Service | Module | Chức năng |
|---|---|---|
| `ContractService` | HR | Lifecycle management của hợp đồng |
| `AttendanceService` | Attendance | Tính toán chấm công |
| `AttendanceProcessingService` | Attendance | Xử lý RawLog → AttendanceBucket |
| `AttendanceCalculator` | Attendance | Tính overtime, late minutes (timezone-aware, inject `TimeZoneInfo` singleton) |
| `ShiftService` | Attendance | Quản lý ca làm việc |
| `WorkingDayCalculator` | Payroll | Tính số ngày làm việc thực tế trong chu kỳ (trừ lễ, cuối tuần) |
| `LeaveTypeService` | Leave | Quản lý loại nghỉ phép |
| `LeaveAllocationService` | Leave | Phân bổ ngày phép tự động |
| `PayrollService` | Payroll | CRUD bảng lương |
| `PayrollProcessingService` | Payroll | Tính toán lương tự động (PIT, BHXH, ...) |
| `PayrollDataProvider` | Payroll | Cung cấp dữ liệu đầu vào cho tính lương |
| `PayrollCycleService` | Payroll | Quản lý chu kỳ lương (tạo, đóng, xem trạng thái) |
| `NotificationService` | Notifications | Gửi/đọc thông báo |
| `AuditLogService` | System | Ghi audit log |
| `SystemSettingService` | System | Đọc/ghi cài đặt hệ thống từ MongoDB |
| `DashboardService` | Dashboard | Tổng hợp KPI từ nhiều provider |

**Dashboard Providers** (Strategy Pattern):
- `HrDashboardProvider` — KPI nhân sự
- `LeaveDashboardProvider` — KPI nghỉ phép
- `RecruitmentDashboardProvider` — KPI tuyển dụng

#### 3.2.4 Dependencies

```xml
MediatR 12.4.1
FluentValidation 11.9.0
Microsoft.Extensions.* abstractions (Configuration, DI, Logging)
```

> **Lưu ý:** `QuestPDF` và `ClosedXML` **không** nằm ở Application layer — chúng nằm ở **Infrastructure** (xem mục 3.3). Application layer chỉ khai báo interface `IPayslipService` và `IExcelExportService`.

---

### 3.3 Layer: Infrastructure

**Project:** `Employee.Infrastructure`

Layer này implement tất cả interfaces từ Domain và Application, phụ thuộc vào các external systems.

#### 3.3.1 Database — MongoDB

- **Driver:** `MongoDB.Driver 3.6.0`
- **Pattern:** Repository Pattern + Unit of Work
- **Context:** `MongoContext` — wrap `IMongoDatabase`, cung cấp `GetCollection<T>()`
- **Unit of Work:** `UnitOfWork` — quản lý transactions (MongoDB sessions)
- **Soft Delete Filter:** `SoftDeleteFilter` — tự động lọc `IsDeleted = false` ở mọi query
- **Class Mapping:** `MongoClassMapConfig` + `MongoMappingConfig` — cấu hình BSON mapping, convention, ObjectId → string

**Collections tương ứng với Entities:**

| Collection | Entity | Đặc biệt |
|---|---|---|
| `employees` | `EmployeeEntity` | Embedded PersonalInfo, JobDetails, BankDetails |
| `attendance_buckets` | `AttendanceBucket` | Bucket pattern: 1 doc / (employee, month) |
| `raw_attendance_logs` | `RawAttendanceLog` | Buffer input trước khi xử lý |
| `leave_requests` | `LeaveRequest` | |
| `leave_allocations` | `LeaveAllocation` | |
| `leave_types` | `LeaveType` | |
| `payrolls` | `PayrollEntity` | Có EmployeeSnapshot embedded |
| `payroll_cycles` | `PayrollCycle` | |
| `public_holidays` | `PublicHoliday` | |
| `performance_reviews` | `PerformanceReview` | |
| `performance_goals` | `PerformanceGoal` | |
| `contracts` | `Contract` | |
| `job_vacancies` | `JobVacancy` | |
| `candidates` | `Candidate` | |
| `interviews` | `Interview` | |
| `departments` | `Department` | |
| `positions` | `Position` | |
| `shifts` | `Shift` | |
| `notifications` | `Notification` | |
| `audit_logs` | `AuditLog` | |
| `system_settings` | `SystemSetting` | |

#### 3.3.2 Identity — ASP.NET Core Identity + MongoDB

- **Library:** `AspNetCore.Identity.MongoDbCore 7.0.0`
- `ApplicationUser` kế thừa `MongoIdentityUser<Guid>` — lưu trong collection `users`
- `ApplicationRole` kế thừa `MongoIdentityRole<Guid>` — lưu trong collection `roles`
- `RefreshTokenEntry` — lưu refresh token (embedded trong user hoặc collection riêng)

**Roles hệ thống:**

| Role | Quyền hạn |
|---|---|
| `Admin` | Toàn quyền hệ thống |
| `HR` | Quản lý nhân sự, lương, nghỉ phép |
| `Manager` | Xem team, duyệt đơn, xem attendance |
| `Employee` | Self-service (check-in, xin nghỉ, xem profile) |

#### 3.3.3 Caching — Redis

- **Library:** `Microsoft.Extensions.Caching.StackExchangeRedis 8.0.8`
- **Service:** `CacheService` — wrapper với TTL, fallback graceful khi Redis unavailable
- **Instance Name Prefix:** `HRM_`
- **Support:** cả Redis URI scheme (`rediss://`) cho Upstash cloud và connection string thông thường

#### 3.3.4 Background Jobs — Hangfire

- **Library:** `Hangfire 1.8.17` + `Hangfire.Redis.StackExchange`
- **Storage:** Redis (Hangfire jobs được persist vào Redis)
- **Service:** `HangfireBackgroundJobService`

Ngoài Hangfire còn có **ASP.NET Core Hosted Services** (`IHostedService`):

| Background Service | Tần suất | Nhiệm vụ |
|---|---|---|
| `LeaveAccrualBackgroundService` | Mỗi 6 giờ | Tự động cộng ngày phép theo chu kỳ |
| `PayrollBackgroundService` | Mỗi 12 giờ | Tự động tính lương khi chu kỳ kết thúc |
| `ContractExpirationBackgroundService` | Mỗi 24 giờ | Cảnh báo/xử lý hợp đồng sắp hết hạn |
| `SoftDeleteCleanupBackgroundService` | Hàng đêm | Hard-delete records soft-deleted > 90 ngày |
| `AttendanceProcessingBackgroundJob` | Mỗi 5 phút | Xử lý RawAttendanceLog → AttendanceBucket |

#### 3.3.5 Email Service

Strategy pattern cho email:

```
IEmailService (interface)
├── SmtpEmailService    (development/production via SMTP)
└── SendGridEmailService (production via SendGrid API)
```

Cấu hình tự động chọn provider dựa trên environment.

#### 3.3.6 File Storage

```
IFileService (interface)
└── FileService
    ├── LocalDisk storage (development)
    └── Supabase Storage  (production: SupabaseStorageOptions)
```

#### 3.3.7 Other Infrastructure Services

| Service | Mô tả |
|---|---|
| `TokenService` | Sinh JWT access token + refresh token |
| `IdentityService` | Đăng ký user, đổi mật khẩu, reset mật khẩu |
| `DateTimeProvider` | Abstraction cho `DateTime.UtcNow` (dễ mock trong test) |
| `PasswordHasher` | BCrypt password hashing (`BCrypt.Net-Next`) |
| `PayslipService` | Xuất PDF phiếu lương (dùng `QuestPDF 2026.2.1`) |
| `ExcelExportService` | Xuất Excel báo cáo lương/attendance (dùng `ClosedXML 0.105.0`) |
| `AccountProvisioningJob` | Tự động tạo tài khoản Identity khi nhân viên được tạo |

---

### 3.4 Layer: API (Presentation)

**Project:** `Employee.API` — .NET 8 Minimal API với Carter.

#### 3.4.1 Endpoint Structure

Sử dụng **Carter** để tổ chức Minimal API endpoints theo module:

```
Endpoints/
├── Auth/
├── HumanResource/
├── Organization/
├── Recruitment/
├── Attendance/
├── Leave/
├── Payroll/
├── Performance/
├── Notifications/
├── Common/         (shared endpoints: upload, settings...)
└── Dev/            (development-only endpoints)
```

Mỗi module có một class implement `ICarterModule`:
```csharp
public class EmployeeEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/employees")
                       .WithTags("Employees")
                       .RequireAuthorization();
        // ...
    }
}
```

#### 3.4.2 API Versioning

- Prefix URL: `/api/v1/...`
- Header: `X-Api-Version: 1`
- Default version: `v1`

#### 3.4.3 Middlewares

| Middleware / Filter | Nhiệm vụ |
|---|---|
| `GlobalExceptionHandler` | Bắt toàn bộ unhandled exception, trả về chuẩn ProblemDetails |
| `SecurityHeadersMiddleware` | Thêm security headers (X-Frame-Options, CSP, HSTS, ...) |
| `HangfireAuthFilter` | Bảo vệ Hangfire Dashboard (`/hangfire`) — chỉ cho phép role `Admin` |

#### 3.4.4 Cross-Cutting Infrastructure trong API

| Feature | Implementation |
|---|---|
| **Logging** | Serilog (Console + File sink, rolling daily, 7 ngày retention) |
| **Correlation ID** | `CorrelationIdProvider` — sinh/truyền X-Correlation-ID qua request |
| **Current User** | `CurrentUserService` — đọc claims từ `HttpContext` |
| **Rate Limiting** | `PartitionedRateLimiter` — per-IP cho auth endpoints (10 req/min), per-user cho API (60 req/min) |
| **CORS** | Policy `AllowAngularApp` — whitelist cấu hình qua `CorsSettings:AllowedOrigins` |
| **Health Checks** | `/health` endpoint — kiểm tra MongoDB connectivity |
| **Response Compression** | Brotli + GZip |
| **Swagger/OpenAPI** | Swagger UI + JWT Bearer auth |

#### 3.4.5 Response Standard

Mọi response theo chuẩn `ApiResponse<T>`:

```json
{
  "succeeded": true,
  "message": "Success",
  "data": { ... },
  "errors": null,
  "errorCode": null
}
```

---

## 4. Frontend — Angular SPA

**Project:** `HRMS-UI`  
**Framework:** Angular 17 (Standalone Components)

### 4.1 Tech Stack Frontend

| Library | Phiên bản | Mục đích |
|---|---|---|
| Angular | 17.3 | Core framework |
| PrimeNG | 17 | UI Component Library |
| TailwindCSS | 3.4 | Utility-first CSS |
| Chart.js | 4.5 | Charts & visualization |
| RxJS | 7.8 | Reactive programming |
| Leaflet | 1.9 | Map (org chart?) |

### 4.2 Cấu trúc thư mục

```
src/app/
├── app.routes.ts          (routing configuration)
├── app.config.ts          (application providers)
│
├── core/                  (singleton services, guards)
│   ├── guards/
│   │   ├── auth.guard.ts  (kiểm tra đăng nhập)
│   │   └── role.guard.ts  (kiểm tra role)
│   ├── interceptors/
│   │   ├── jwt.interceptor.ts    (đính kèm Bearer token)
│   │   └── error.interceptor.ts  (xử lý lỗi toàn cục)
│   ├── models/            (TypeScript interfaces/types)
│   └── services/          (core services)
│       ├── auth.service.ts
│       ├── notification.service.ts
│       ├── toast.service.ts
│       ├── theme.service.ts
│       └── ...
│
├── features/              (feature modules)
│   ├── auth/
│   ├── dashboard/
│   ├── employee/
│   ├── organization/
│   ├── recruitment/
│   ├── attendance/
│   ├── leave/
│   ├── payroll/
│   ├── performance/
│   ├── system/
│   └── not-found/
│
├── layout/                (shared layout)
│   ├── main-layout/       (wrapper cho authenticated routes)
│   ├── navbar/
│   ├── sidebar/
│   └── footer/
│
└── shared/                (reusable UI components)
    ├── components/
    ├── directives/
    ├── pipes/
    └── utils/
```

### 4.3 Routing & Access Control

Routing sử dụng **lazy loading** cho toàn bộ page components (code splitting tối ưu bundle size):

```
/ (root)
├── login                                   (public)
├── forgot-password                         (public)
├── reset-password                          (public)
├── change-password                         [authGuard]
└── [MainLayoutComponent]                   [canActivate: authGuard]
    ├── dashboard
    ├── employees                           [all authenticated]
    ├── employees/add                       [Admin, HR]
    ├── employees/:id                       [Admin, HR, Manager]  ← alias cho profile
    ├── employee-profile/:id                [Admin, HR, Manager]
    ├── profile                             [all authenticated]  ← self-service
    ├── directory
    ├── org-chart
    ├── departments                         [Admin, HR]
    ├── system/positions                    [Admin, HR]
    ├── system/users                        [Admin]
    ├── system/audit-logs                   [Admin]
    ├── recruitment                         [Admin, HR]
    ├── recruitment/candidates/:id          [Admin, HR]  ← xem chi tiết ứng viên
    ├── attendance                          [Admin, HR, Manager]
    ├── attendance/shifts                   [Admin, HR, Manager]
    ├── attendance/shifts/add               [Admin, HR, Manager]
    ├── attendance/shifts/edit/:id          [Admin, HR, Manager]
    ├── attendance/check-in                 [all authenticated]
    ├── attendance/my-history               [all authenticated]
    ├── payroll                             [Admin, HR, Manager]
    ├── payroll/tax-report                  [Admin, HR]
    ├── leaves                              [all authenticated]
    ├── approvals                           [Admin, HR, Manager]
    ├── admin/leave-reports                 [Admin, HR]
    ├── performance                         [Admin, HR, Manager]
    │
    │  ── Navigation Aliases (redirect) ──
    ├── time-tracking  →  attendance
    └── tasks          →  approvals
```

### 4.4 HTTP Communication

- **JWT Interceptor:** Tự động đính kèm `Authorization: Bearer <token>` vào mọi request
- **Error Interceptor:** Xử lý HTTP errors toàn cục (401 redirect to login, 500 toast, ...)
- **Base URL:** Cấu hình qua `environments/environment.ts`

---

## 5. Data Architecture

### 5.1 Database: MongoDB 7

HRMS chọn **MongoDB** (document database) vì:
- Nhân viên có cấu trúc phức tạp và đa dạng (embedded documents)
- Schema linh hoạt cho các loại leave, performance review khác nhau
- Attendance bucket pattern: hiệu quả cho read/write chấm công theo tháng

### 5.2 Attendance Bucket Pattern

Thay vì mỗi lần check-in/check-out là 1 document riêng, hệ thống dùng **Bucket Pattern**:

```
AttendanceBucket {
  employeeId: "abc",
  month: "03-2026",       // 1 document / (employee, month)
  dailyLogs: [            // embedded array
    { date: "2026-03-01", checkIn: "08:00", checkOut: "17:30", status: "Present", overtimeHours: 0.5 },
    { date: "2026-03-02", checkIn: "08:15", checkOut: "17:00", status: "Late",    overtimeHours: 0 },
    ...
  ],
  totalPresent: 20,
  totalLate: 2,
  totalOvertime: 3.5
}
```

**Lợi ích:** Giảm số lượng documents, tăng performance aggregate query tính lương.

### 5.3 Soft Delete Strategy

Mọi entity đều dùng **soft delete**:
- `MarkDeleted()` set `IsDeleted = true` — không xóa khỏi DB
- `SoftDeleteFilter` tự động loại bỏ `IsDeleted = true` khỏi mọi MongoDB query
- `SoftDeleteCleanupBackgroundService` hard-delete records soft-deleted **> 90 ngày**

### 5.4 Payroll Snapshot Pattern

Khi tính lương, hệ thống **snapshot** thông tin nhân viên tại thời điểm đó vào `PayrollEntity.Snapshot`, đảm bảo lịch sử lương không bị ảnh hưởng khi thông tin nhân viên thay đổi sau này.

---

## 6. Authentication & Authorization

### 6.1 Authentication Flow

```
Client                    API                      Database
  │                        │                           │
  │──POST /auth/login──────►│                           │
  │                        │──find user by email───────►│
  │                        │◄──user data───────────────│
  │                        │                           │
  │                        │  verify BCrypt password   │
  │                        │  generate JWT (60 min)    │
  │                        │  generate Refresh Token   │
  │◄──{ accessToken,       │                           │
  │     refreshToken }─────│                           │
  │                        │                           │
  │──GET /api/v1/employees──►│                          │
  │  Authorization: Bearer  │                           │
  │                        │  validate JWT             │
  │                        │  extract claims           │
  │◄──200 OK───────────────│                           │
```

### 6.2 JWT Configuration

| Setting | Giá trị |
|---|---|
| Algorithm | HMAC-SHA256 (`HS256`) |
| Issuer | `EmployeeAPI` |
| Audience | `EmployeeClient` |
| Duration | 60 phút (dev) / 30 phút (prod) |
| Key | Min 32 ký tự — inject qua env var hoặc User Secrets |

### 6.3 Security Controls

| Control | Implementation |
|---|---|
| **Password Policy** | Min 8 ký tự, phải có chữ hoa, chữ thường, số |
| **Account Lockout** | 5 lần sai → lock 15 phút |
| **Rate Limiting** | Auth: 10 req/min/IP; API: 60 req/min/user |
| **Security Headers** | X-Frame-Options, X-Content-Type, CSP, HSTS |
| **CORS** | Whitelist origins; credentials allowed |
| **JWT Key Validation** | Fail-fast tại startup nếu key là placeholder |

---

## 7. Background Jobs & Event System

### 7.1 Kiến trúc xử lý Attendance

Do check-in/check-out là hot path (nhiều request đồng thời), hệ thống tách thành 2 bước:

```
Step 1: Check-in/out endpoint
    │  Chỉ lưu RawAttendanceLog (nhanh)
    ▼
Raw Attendance Log (buffer)

Step 2: AttendanceProcessingBackgroundJob (cứ 5 phút)
    │  Đọc unprocesed RawLogs
    │  Tính AttendanceBucket (timezone-aware, overtime, late)
    │  Cập nhật/tạo AttendanceBucket
    ▼
AttendanceBucket (final)
```

**Lợi ích:** Check-in endpoint cực kỳ nhanh, không block user; xử lý phức tạp được offload sang background.

### 7.2 Payroll Automation

```
PayrollBackgroundService (mỗi 12 giờ)
    │
    │  Kiểm tra PayrollCycles có status = Open và đến ngày đóng
    ▼
PayrollProcessingService
    │  1. Lấy danh sách nhân viên active
    │  2. Lấy AttendanceBucket của tháng
    │  3. Lấy LeaveRequest đã approved
    │  4. Tính GrossIncome, BHXH (8%), BHYT (1.5%), BHTN (1%), PIT
    │  5. Áp dụng overtime pay
    │  6. Trừ unpaid leave days
    │  7. Tính FinalNetSalary
    ▼
PayrollEntity (status = Calculated)
```

### 7.3 Leave Accrual

```
LeaveAccrualBackgroundService (mỗi 6 giờ)
    │
    │  Kiểm tra LeaveAllocation cần được cộng thêm (theo cấu hình LeaveType)
    │  Cộng accrued days theo tỷ lệ tháng/năm
    ▼
LeaveAllocation (updated balance)
```

---

## 8. Caching Strategy

Hệ thống sử dụng **Redis** làm L2 Cache với tiền tố `HRM_`:

| Cache Key Pattern | TTL | Dữ liệu |
|---|---|---|
| `HRM_employees_list` | 5 phút | Danh sách nhân viên |
| `HRM_departments` | 15 phút | Danh sách phòng ban |
| `HRM_positions` | 15 phút | Danh sách chức vụ |
| `HRM_leave_types` | 30 phút | Danh sách loại nghỉ phép |
| `HRM_settings` | 10 phút | System settings |

**Resilience:** Cấu hình `AbortOnConnectFail = false` — nếu Redis không available, hệ thống **tự động fallback** về database mà không crash.

**TTL thấp** được cố ý set để tránh stale data, đặc biệt quan trọng với employee list (thay đổi thường xuyên).

---

## 9. Cross-Cutting Concerns

### 9.1 Logging

Sử dụng **Serilog** với enrichers và structured logging:

```
[08:30:15 INF] abc-123 | Handling CreateEmployeeCommand — FullName: Nguyen Van A
[08:30:15 INF] abc-123 | POST /api/v1/employees completed in 145ms — 201 Created
```

| Environment | Sinks | Retention |
|---|---|---|
| Development | Console + File | 7 ngày |
| Production | Console + File | 30 ngày |

**Enriched fields:** `CorrelationId`, `UserId`, `RequestName`, `MachineName`, `ThreadId`.

### 9.2 Error Handling

```
Exception thrown
    │
    ▼
GlobalExceptionHandler (IExceptionHandler)
    │
    ├── ValidationException  → 400 Bad Request  + field errors
    ├── NotFoundException    → 404 Not Found
    ├── UnauthorizedException→ 401 Unauthorized
    ├── ForbiddenException   → 403 Forbidden
    └── Exception (catch-all)→ 500 Internal Server Error + CorrelationId
```

Tất cả errors được trả về theo chuẩn **RFC 7807 ProblemDetails**.

### 9.3 Audit Logging

Mọi write operation (thêm/sửa/xóa) được ghi vào `AuditLog` collection thông qua `AuditLogService`:

```json
{
  "action": "UPDATE_EMPLOYEE",
  "entityId": "...",
  "entityType": "EmployeeEntity",
  "performedBy": "user-id",
  "performedAt": "2026-03-06T08:30:00Z",
  "changes": { "before": {...}, "after": {...} }
}
```

### 9.4 Validation

Hai lớp validation:

| Lớp | Công nghệ | Phạm vi |
|---|---|---|
| **API Layer** | `MiniValidation` | Input validation tại endpoint (nhanh, trước MediatR) |
| **Application Layer** | `FluentValidation` | Business rule validation trong pipeline behavior |

---

## 10. Infrastructure & Deployment

### 10.1 Docker Architecture

Hệ thống được containerize hoàn toàn với **Docker Compose**:

```
docker-compose.yml
├── app_db          (MongoDB 7)        — internal only (expose 27017)
│     └── volume: mongo_data
├── app_cache       (Redis Alpine)     — internal only (expose 6379)
├── app_backend     (.NET 8 API)       — port 5000:8080
│     ├── depends_on: app_db (healthy)
│     └── volume: upload_data → /app/wwwroot/uploads
└── app_frontend    (Angular + Nginx)  — port 80:80
      └── depends_on: app_backend

Network: employee_network (bridge)
```

**Security:** MongoDB và Redis chỉ accessible trong nội bộ Docker network, không expose ra host.

### 10.2 Environment Configuration

Secrets được inject qua **environment variables** (không hardcode):

| Variable | Secret | Override |
|---|---|---|
| `EmployeeDatabaseSettings__ConnectionString` | MongoDB URI | `.env` file |
| `JwtSettings__Key` | JWT signing key | `.env` / User Secrets |
| `RedisSettings__ConnectionString` | Redis URI | `.env` file |
| `MONGO_USER`, `MONGO_PASSWORD` | DB credentials | `.env` file |

### 10.3 Production Deployment

| Service | Platform |
|---|---|
| Backend API | Docker (self-hosted) / Render |
| Frontend | Vercel (`hrms-clean-arch-frontend.vercel.app`) |
| Database | MongoDB Atlas (cloud) |
| Cache | Upstash Redis (cloud, `rediss://`) |
| Email | SMTP Gmail / SendGrid |
| File Storage | Supabase Storage |

### 10.4 Health Checks

- Endpoint: `GET /health`
- Checks: MongoDB ping
- Response format: JSON (HealthChecks middleware)

---

## 11. Testing Strategy

### 11.1 Test Projects

| Project | Type | Framework |
|---|---|---|
| `Employee.UnitTests` | Unit Tests | xUnit / NUnit |
| `Employee.IntegrationTests` | Integration Tests | `WebApplicationFactory` |

### 11.2 Unit Tests Structure

```
Employee.UnitTests/
├── Domain/         (entity business rules, value objects)
├── Application/    (command/query handlers, validators)
└── Features/       (use-case specific tests)
```

### 11.3 Integration Tests

`EmployeeApiFactory` dùng `WebApplicationFactory<Program>` để spin up test server:
- Database: MongoDB test instance (testcontainers hoặc in-memory mock)
- Authentication: test JWT token generation
- `ApiCollection.cs` — xUnit collection fixture để share factory instance

---

## 12. Dependency Map

```
Employee.API
    ├── Employee.Application
    │       └── Employee.Domain
    └── Employee.Infrastructure
            ├── Employee.Application
            └── Employee.Domain
```

### 12.1 Key Package Versions

| Package | Version | Layer |
|---|---|---|
| .NET | 8.0 | All |
| Carter | 8.2.1 | API |
| MediatR | 12.4.1 | Application |
| FluentValidation | 11.9.0 | Application |
| MongoDB.Driver | 3.6.0 | Infrastructure |
| MongoDB.Bson | 3.6.0 | Domain |
| AspNetCore.Identity.MongoDbCore | 7.0.0 | Infrastructure |
| Hangfire | 1.8.17 | Infrastructure (API + Infrastructure) |
| Hangfire.Redis.StackExchange | 1.9.0 | Infrastructure |
| StackExchangeRedis | 8.0.8 | Infrastructure |
| BCrypt.Net-Next | 4.0.3 | Infrastructure |
| SendGrid | 9.29.3 | Infrastructure |
| QuestPDF | 2026.2.1 | Infrastructure |
| ClosedXML | 0.105.0 | Infrastructure |
| System.IdentityModel.Tokens.Jwt | 8.15.0 | Infrastructure |
| Serilog.AspNetCore | 10.0.0 | API |
| MiniValidation | 0.9.2 | API |
| Angular | 17.3 | Frontend |
| PrimeNG | 17.18 | Frontend |
| TailwindCSS | 3.4 | Frontend |

---

## Appendix: Coding Conventions

| Convention | Rule |
|---|---|
| Entities | Private setters, factory constructor, domain methods |
| Repositories | Async methods, return Domain entities (not DTOs) |
| Commands/Queries | Immutable record types |
| Responses | Wrapped in `ApiResponse<T>` |
| Errors | Custom exceptions (`NotFoundException`, `ValidationException`, ...) |
| JSON | camelCase (cấu hình global) |
| IDs | `string` trong Domain, MongoDB ObjectId trong DB |
| Soft Delete | Luôn dùng `MarkDeleted()` — không gọi delete trực tiếp |
| Timezone | `Asia/Ho_Chi_Minh` (IANA, UTC+7) — cấu hình qua `SystemSettings:TimezoneId`. Fallback Windows: `SE Asia Standard Time`. Inject làm `TimeZoneInfo` singleton qua DI. |
