# DEVELOPER GUIDE
# Hướng Dẫn Phát Triển — EmployeeCleanArch HRM

---

## 1. PREREQUISITES (Yêu Cầu Cài Đặt)

### Bắt buộc

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop |
| Git | Latest | https://git-scm.com |

### Khuyến nghị

| Tool | Mục đích |
|------|---------|
| Visual Studio 2022 / VS Code | IDE chính |
| MongoDB Compass | GUI quản lý database |
| Redis Insight | GUI quản lý Redis cache |
| Postman / Thunder Client | Test API |

---

## 2. PROJECT SETUP

### Bước 1: Clone project

```bash
git clone <repo-url>
cd EmployeeCleanArch
```

### Bước 2: Khởi động Infrastructure (MongoDB + Redis)

```bash
# Chạy MongoDB + Redis bằng Docker
docker-compose up -d app_db app_cache
```

Kiểm tra:
- MongoDB: `mongodb://localhost:27017` (Mở MongoDB Compass)
- Redis: `localhost:6379` (Mở Redis Insight)

### Bước 3: Chạy API

**Option A — Visual Studio:**
- Mở `EmployeeCleanArch.sln`
- Set Employee.API là Startup Project
- F5 (Debug) hoặc Ctrl+F5 (Run)

**Option B — Terminal:**
```bash
cd Employee.API
dotnet run
```

**Option C — Docker (toàn bộ):**
```bash
docker-compose up --build
```

### Bước 4: Kiểm tra

- API: http://localhost:5000/swagger
- Swagger UI sẽ hiện tất cả endpoints
- Database sẽ auto-seed data mẫu (admin account, roles, settings...)

### Default Admin Account

```
Username: admin
Password: <configured-in-appsettings.json> (default: User@12345)
```

---

## 3. PROJECT STRUCTURE

```
EmployeeCleanArch/
│
├── EmployeeCleanArch.sln          # Solution file
├── docker-compose.yml             # Docker orchestration
│
├── Employee.Domain/               # LAYER 1: Domain (innermost)
│   └── Entities/
│       ├── Common/                # BaseEntity, ApplicationUser, ApplicationRole
│       ├── Attendance/            # Shift, AttendanceBucket, RawAttendanceLog
│       ├── HumanResource/         # Employee, Contract, Candidate, Interview, JobVacancy
│       ├── Leave/                 # LeaveType, LeaveAllocation, LeaveRequest
│       ├── Organization/          # Department, Position
│       ├── Payroll/               # PayrollEntity
│       └── ValueObjects/          # PersonalInfo, JobDetails, BankDetails, DailyLog,
│                                  # SalaryComponents, SalaryRange, EmployeeSnapshot
│
├── Employee.Application/          # LAYER 2: Application
│   ├── DependencyInjection.cs     # MediatR + FluentValidation registration
│   ├── Common/                    # Interfaces (Repos, Services, UoW, Cache)
│   └── Features/                  # Organized by module
│       ├── Auth/
│       │   ├── Commands/          # Login, Register, ChangePassword, AssignRole...
│       │   └── Queries/           # GetUsers, GetRoles...
│       ├── HumanResource/
│       │   ├── Commands/          # CreateEmployee, UpdateEmployee, DeleteEmployee
│       │   ├── DTOs/              # EmployeeDto, ContractDto, CreateEmployeeCommand
│       │   ├── EventHandlers/     # CreateUserEventHandler
│       │   ├── Mappers/           # Entity ↔ DTO mapping
│       │   ├── Services/          # EmployeeService, ContractService
│       │   └── Validators/        # FluentValidation rules
│       ├── Attendance/
│       │   ├── DTOs/
│       │   ├── Logic/             # AttendanceCalculator (pure logic)
│       │   └── Services/          # AttendanceService, AttendanceProcessingService
│       ├── Leave/
│       │   ├── DTOs/
│       │   ├── EventHandlers/     # InitializeLeaveOnContractHandler
│       │   └── Services/          # LeaveRequestService, LeaveAllocationService
│       ├── Payroll/
│       │   ├── DTOs/
│       │   └── Services/          # PayrollService, PayrollProcessingService
│       ├── Organization/
│       │   └── Services/          # DepartmentService, PositionService
│       └── Common/
│           └── Services/          # AuditService, FileService
│
├── Employee.Infrastructure/       # LAYER 3: Infrastructure
│   ├── DependencyInjection.cs     # All infrastructure registrations
│   ├── Persistence/
│   │   ├── MongoContext.cs         # MongoDB connection + collections
│   │   └── Repositories/          # GenericRepository, specific repos
│   ├── Services/
│   │   ├── TokenService.cs        # JWT token generation
│   │   ├── CacheService.cs        # Redis cache operations
│   │   └── UnitOfWork.cs          # MongoDB transaction management
│   └── BackgroundServices/        # LeaveAccrual, AttendanceProcessing
│
├── Employee.API/                  # LAYER 4: Presentation (outermost)
│   ├── Program.cs                 # App configuration, DI, middleware pipeline
│   ├── Endpoints/                 # Carter modules (Minimal API endpoints)
│   │   ├── AuthEndpoints.cs
│   │   ├── EmployeeEndpoints.cs
│   │   ├── ContractEndpoints.cs
│   │   ├── AttendanceEndpoints.cs
│   │   ├── LeaveEndpoints.cs
│   │   ├── PayrollEndpoints.cs
│   │   ├── DepartmentEndpoints.cs
│   │   └── PositionEndpoints.cs
│   ├── Middlewares/
│   ├── Common/                    # ValidationFilter, Seeder
│   └── appsettings.json           # Configuration
│
├── Employee.UnitTests/            # Unit tests
│
└── docs/                          # Documentation
```

---

## 4. ARCHITECTURE RULES (QUY TẮC KIẾN TRÚC)

### Dependency Rule (BẮT BUỘC)

```
Domain ← Application ← Infrastructure ← API

Luồng phụ thuộc ONLY từ ngoài vào trong:
- Domain: KHÔNG phụ thuộc bất cứ gì
- Application: Chỉ phụ thuộc Domain
- Infrastructure: Phụ thuộc Application + Domain
- API: Phụ thuộc tất cả
```

### Khi tạo Feature mới

1. **Domain**: Tạo Entity + Value Objects (nếu cần)
2. **Application**: Tạo DTOs → Services/Handlers → Validators → Mappers
3. **Infrastructure**: Tạo Repository (nếu cần collection mới)
4. **API**: Tạo Carter Endpoint module

---

## 5. CODING CONVENTIONS

### Naming

| Loại | Convention | Ví dụ |
|------|-----------|-------|
| Class | PascalCase | `EmployeeService` |
| Interface | IPascalCase | `IEmployeeRepository` |
| Method | PascalCase | `GetByIdAsync` |
| Property | PascalCase | `EmployeeCode` |
| Variable | camelCase | `employeeId` |
| Constant | PascalCase | `DefaultPageSize` |
| File name | Tên class | `EmployeeService.cs` |
| Folder | PascalCase | `HumanResource/` |

### Async Convention

- Tất cả methods truy cập DB/cache PHẢI async
- Tên method kết thúc bằng `Async`: `GetByIdAsync`, `CreateAsync`

### Service Convention

```csharp
// Interface đặt trong Application/Common/Interfaces/
public interface IEmployeeService
{
    Task<EmployeeDto?> GetByIdAsync(string id, string currentUserId, IList<string> roles);
    Task<PagedResult<EmployeeListDto>> GetPagedAsync(PagedRequest request);
}

// Implementation đặt trong Application/Features/{Module}/Services/
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    // Constructor injection...
}
```

### CQRS Convention (MediatR)

```csharp
// Command (Write) — Application/Features/{Module}/Commands/{Action}/
public record CreateEmployeeCommand(...) : IRequest<Result<string>>;
public class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, Result<string>>

// Query (Read) — Application/Features/{Module}/Queries/{Action}/
public record GetUsersQuery(...) : IRequest<Result<PagedResult<UserDto>>>;
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, ...>

// Event - Application/Features/{Module}/EventHandlers/
public class EmployeeCreatedEvent : INotification { ... }
public class CreateUserEventHandler : INotificationHandler<EmployeeCreatedEvent>
```

### Endpoint Convention (Carter)

```csharp
// API/Endpoints/{Module}Endpoints.cs
public class EmployeeEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees")
            .RequireAuthorization()
            .WithTags("Employees");

        group.MapGet("/{id}", GetById);
        group.MapPost("/", Create).RequireAuthorization("AdminOrHR");
    }
}
```

### Validation Convention (FluentValidation)

```csharp
// Application/Features/{Module}/Validators/
public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
    }
}
// Validator auto-registered via DI scan
// Auto-applied via ValidationFilter<T> in endpoints
```

---

## 6. CONFIGURATION

### appsettings.json

```json
{
  "EmployeeDatabaseSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "EmployeeCleanDB"
  },
  "RedisSettings": {
    "ConnectionString": "localhost:6379"
  },
  "JwtSettings": {
    "Key": "Your_Secret_Key_Must_Be_At_Least_32_Characters",
    "Issuer": "EmployeeAPI",
    "Audience": "EmployeeClient"
  },
  "Seeding": {
    "DefaultPassword": "User@12345"
  }
}
```

### Docker Compose overrides

Docker Compose sử dụng environment variables để override appsettings:
- `EmployeeDatabaseSettings__ConnectionString=mongodb://app_db:27017`
- `RedisSettings__ConnectionString=app_cache:6379`

---

## 7. COMMON DEVELOPMENT TASKS

### Tạo Entity mới

1. Tạo class trong `Employee.Domain/Entities/{Module}/`
2. Kế thừa `BaseEntity`
3. Thêm collection vào `MongoContext.cs`
4. Tạo Repository (nếu cần custom queries)
5. Đăng ký DI trong `Infrastructure/DependencyInjection.cs`

### Tạo API Endpoint mới

1. Tạo DTO trong `Application/Features/{Module}/DTOs/`
2. Tạo Service interface + implementation
3. Tạo Validator
4. Đăng ký Service trong `Infrastructure/DependencyInjection.cs`
5. Tạo Carter module trong `API/Endpoints/`

### Tạo Command/Query mới (CQRS)

1. Tạo Command/Query record: `public record XxxCommand(...) : IRequest<Result<T>>;`
2. Tạo Handler: `public class XxxHandler : IRequestHandler<XxxCommand, Result<T>>`
3. MediatR auto-discover — không cần đăng ký thêm

### Tạo Event Handler mới

1. Tạo Event: `public class XxxEvent : INotification`
2. Tạo Handler: `public class XxxEventHandler : INotificationHandler<XxxEvent>`
3. Publish từ service: `await _mediator.Publish(new XxxEvent { ... });`

### Thêm Cache cho data

1. Mở `CacheService` (hoặc dùng `ICacheService`)
2. Set: `await _cache.SetAsync<T>(key, value, ttl);`
3. Get: `var data = await _cache.GetAsync<T>(key);`
4. Remove: `await _cache.RemoveAsync(key);`

---

## 8. DEBUGGING TIPS

### MongoDB

```bash
# Kết nối MongoDB Compass
mongodb://localhost:27017

# Xem collections
Database: EmployeeCleanDB
Collections: employees, contracts, attendance_buckets, payrolls...
```

### Redis

```bash
# Kết nối Redis CLI
docker exec -it employee_redis_cache redis-cli

# Xem tất cả keys
KEYS *

# Xem giá trị
GET DEPARTMENT_TREE
```

### JWT Token

- Decode token tại: https://jwt.io
- Copy token từ Login response → paste vào jwt.io → xem claims

### Common Errors

| Error | Nguyên nhân | Fix |
|-------|------------|-----|
| 401 Unauthorized | Token hết hạn hoặc sai | Login lại |
| 403 Forbidden | Không đủ quyền (role) | Dùng account đúng role |
| Connection refused :27017 | MongoDB chưa chạy | `docker-compose up -d app_db` |
| Connection refused :6379 | Redis chưa chạy | `docker-compose up -d app_cache` |
| Validation error | DTO không hợp lệ | Xem response message |

---

## 9. GIT WORKFLOW

### Branch Naming

| Type | Format | Ví dụ |
|------|--------|-------|
| Feature | `feature/{module}-{description}` | `feature/leave-balance-check` |
| Bugfix | `bugfix/{bug-id}-{description}` | `bugfix/BUG-1-insurance-calc` |
| Hotfix | `hotfix/{description}` | `hotfix/payroll-deduction` |

### Commit Message

```
[MODULE] Action: Description

[PAYROLL] Fix: Calculate insurance on baseSalary instead of grossIncome
[LEAVE] Add: Balance check before creating leave request
[AUTH] Fix: Check duplicate email on registration
```

---

## 10. USEFUL COMMANDS

```bash
# Build project
dotnet build

# Run API
cd Employee.API && dotnet run

# Run tests
cd Employee.UnitTests && dotnet test

# Docker — start all
docker-compose up -d

# Docker — rebuild API
docker-compose up --build app_backend

# Docker — view logs
docker logs employee_api -f

# Docker — stop all
docker-compose down

# Docker — reset database
docker-compose down -v  # WARNING: Deletes all data!
```
