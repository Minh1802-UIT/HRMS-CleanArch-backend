# 👨‍💻 Developer Guide — Hướng Dẫn Phát Triển

---

## 1️⃣ Prerequisites

### Bắt buộc

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| Docker Desktop | Latest |
| Git | Latest |

### Khuyến nghị

| Tool | Mục đích |
|------|---------|
| Visual Studio 2022 / VS Code | IDE |
| MongoDB Compass | GUI database |
| Redis Insight | GUI cache |
| Postman | Test API |

---

## 2️⃣ Project Setup

### Bước 1: Clone

```
git clone <repo-url>
cd EmployeeCleanArch
```

### Bước 2: Start MongoDB + Redis

```
docker-compose up -d app_db app_cache
```

### Bước 3: Run API

- **VS Studio**: Mở .sln → F5
- **Terminal**: `cd Employee.API && dotnet run`
- **Docker**: `docker-compose up --build`

### Bước 4: Verify

- Swagger UI: http://localhost:5000/swagger
- Default login: admin / Admin@123

---

## 3️⃣ Project Structure

**Employee.Domain/** — Layer 1 (innermost)
- Entities: Employee, Contract, Shift, AttendanceBucket, LeaveType, LeaveAllocation, LeaveRequest, PayrollEntity, Department, Position
- Value Objects: PersonalInfo, JobDetails, BankDetails, DailyLog, SalaryComponents, SalaryRange, EmployeeSnapshot
- BaseEntity: Id, IsDeleted, CreatedAt, UpdatedAt, Version

**Employee.Application/** — Layer 2
- Features/ (organized by module):
    - Auth/ → Commands (Login, Register...) + Queries
    - HumanResource/ → Commands + DTOs + EventHandlers + Services + Validators
    - Attendance/ → DTOs + Logic (Calculator) + Services
    - Leave/ → DTOs + EventHandlers + Services
    - Payroll/ → DTOs + Services
    - Organization/ → Services
    - Common/ → AuditService, FileService
- Common/ → Interfaces (Repos, Services, UoW, Cache)

**Employee.Infrastructure/** — Layer 3
- Persistence: MongoContext, Repositories
- Services: TokenService, CacheService, UnitOfWork
- BackgroundServices: LeaveAccrual, AttendanceProcessing

**Employee.API/** — Layer 4 (outermost)
- Program.cs: Config, DI, middleware
- Endpoints/: Carter modules (Auth, Employee, Contract, Attendance, Leave, Payroll, Dept, Position)
- Middlewares, Common (ValidationFilter, Seeder)

---

## 4️⃣ Architecture Rules

### Dependency Rule (BẮT BUỘC)

Domain ← Application ← Infrastructure ← API

- Domain: KHÔNG phụ thuộc gì
- Application: Chỉ phụ thuộc Domain
- Infrastructure: Phụ thuộc Application + Domain
- API: Phụ thuộc tất cả

### Khi tạo Feature mới

1. **Domain**: Entity + Value Objects
2. **Application**: DTOs → Services/Handlers → Validators → Mappers
3. **Infrastructure**: Repository (nếu collection mới)
4. **API**: Carter Endpoint module

---

## 5️⃣ Coding Conventions

### Naming

| Loại | Convention | Ví dụ |
|------|-----------|-------|
| Class | PascalCase | EmployeeService |
| Interface | IPascalCase | IEmployeeRepository |
| Method | PascalCase + Async | GetByIdAsync |
| Property | PascalCase | EmployeeCode |
| Variable | camelCase | employeeId |

### CQRS Pattern

**Command** (Write):
- Record: `CreateEmployeeCommand : IRequest<Result<string>>`
- Handler: `CreateEmployeeHandler : IRequestHandler<...>`

**Query** (Read):
- Record: `GetUsersQuery : IRequest<Result<PagedResult<UserDto>>>`
- Handler: `GetUsersQueryHandler : IRequestHandler<...>`

**Event** (Side Effect):
- Event: `EmployeeCreatedEvent : INotification`
- Handler: `CreateUserEventHandler : INotificationHandler<...>`

### Endpoint (Carter)

```csharp
public class EmployeeEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees")
            .RequireAuthorization().WithTags("Employees");
        group.MapGet("/{id}", GetById);
        group.MapPost("/", Create).RequireAuthorization("AdminOrHR");
    }
}
```

---

## 6️⃣ Configuration

### appsettings.json

```json
{
  "EmployeeDatabaseSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "EmployeeCleanDB"
  },
  "RedisSettings": { "ConnectionString": "localhost:6379" },
  "JwtSettings": {
    "Key": "Secret_Key_At_Least_32_Characters",
    "Issuer": "EmployeeAPI",
    "Audience": "EmployeeClient"
  }
}
```

Docker Compose dùng environment variables override.

---

## 7️⃣ Common Dev Tasks

### Tạo Entity mới

1. Domain/Entities/{Module}/ → class kế thừa BaseEntity
2. MongoContext.cs → thêm collection
3. Infrastructure/ → tạo Repository + đăng ký DI

### Tạo API Endpoint

1. Application/ → DTO + Service + Validator
2. Infrastructure/ → đăng ký DI
3. API/Endpoints/ → Carter module

### Tạo Event

1. Application/ → Event class (INotification) + Handler (INotificationHandler)
2. Publish: `await _mediator.Publish(new XxxEvent { ... });`

---

## 8️⃣ Debugging Tips

### Database

- MongoDB Compass: mongodb://localhost:27017 → DB: EmployeeCleanDB

### Redis

- docker exec -it employee_redis_cache redis-cli → KEYS *

### JWT

- Decode tại jwt.io → xem claims

### Common Errors

| Error | Fix |
|-------|-----|
| 401 Unauthorized | Login lại lấy token mới |
| 403 Forbidden | Dùng account đúng role |
| Connection refused :27017 | docker-compose up -d app_db |
| Connection refused :6379 | docker-compose up -d app_cache |

---

## 9️⃣ Git Workflow

### Branches

| Type | Format |
|------|--------|
| Feature | feature/{module}-{desc} |
| Bugfix | bugfix/{id}-{desc} |
| Hotfix | hotfix/{desc} |

### Commit Message

```
[MODULE] Action: Description
[PAYROLL] Fix: Calculate insurance on baseSalary
[LEAVE] Add: Balance check before creating request
```

---

## 🔟 Useful Commands

```
dotnet build                          # Build
cd Employee.API && dotnet run         # Run API
cd Employee.UnitTests && dotnet test  # Run tests
docker-compose up -d                  # Start all
docker-compose up --build app_backend # Rebuild API
docker logs employee_api -f           # View logs
docker-compose down                   # Stop all
docker-compose down -v                # Reset DB (⚠️ deletes data)
```
