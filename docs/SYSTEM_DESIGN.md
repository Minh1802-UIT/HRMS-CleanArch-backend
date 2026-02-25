# SYSTEM DESIGN DOCUMENT
# Hệ Thống Quản Lý Nhân Sự — EmployeeCleanArch HRM

| Thông tin | Chi tiết |
|-----------|----------|
| Phiên bản | 1.0 |
| Ngày tạo | 17/02/2026 |
| Tác giả | Engineering Team |

---

## 1. REQUIREMENTS GATHERING

### 1.1 Functional Requirements (FR)

| # | Requirement | Priority |
|---|------------|----------|
| FR-01 | User authentication (login/register) với JWT Bearer Token | P0 |
| FR-02 | Role-based authorization (Admin, HR, Manager, Employee) | P0 |
| FR-03 | CRUD nhân viên với thông tin cá nhân, công việc, ngân hàng | P0 |
| FR-04 | Quản lý hợp đồng lao động (tạo, cập nhật, chấm dứt, auto-expire) | P0 |
| FR-05 | Chấm công (check-in/out), xử lý log, tính trạng thái ngày công | P0 |
| FR-06 | Quản lý ca làm việc (bao gồm ca qua đêm, grace period) | P0 |
| FR-07 | Quản lý nghỉ phép (loại phép, cấp phát, xin/duyệt, cộng dồn) | P0 |
| FR-08 | Tính lương tự động (BHXH/BHYT/BHTN, thuế TNCN 7 bậc, OT) | P0 |
| FR-09 | Quản lý phòng ban phân cấp (tree, cycle detection) | P1 |
| FR-10 | Quản lý chức vụ phân cấp (tree, salary range) | P1 |
| FR-11 | Sơ đồ tổ chức (Org Chart) | P1 |
| FR-12 | Audit log cho các thao tác quan trọng | P1 |
| FR-13 | System settings dynamic (tỷ lệ BH, mức trần, giảm trừ thuế) | P1 |
| FR-14 | Tuyển dụng (tin tuyển dụng, ứng viên, phỏng vấn) | P2 |

### 1.2 Non-Functional Requirements (NFR)

| # | Requirement | Target | Rationale |
|---|------------|--------|-----------|
| NFR-01 | Response Time | < 500ms (P95) | Trải nghiệm user mượt mà |
| NFR-02 | Availability | 99.5% uptime | Hệ thống nội bộ, không cần 99.99% |
| NFR-03 | Concurrent Users | 50–200 | SME 10–500 NV, peak giờ chấm công |
| NFR-04 | Data Consistency | Strong consistency cho payroll/leave | Tài chính cần chính xác tuyệt đối |
| NFR-05 | Data Retention | 7 năm payroll, 3 năm attendance | Theo quy định lao động VN |
| NFR-06 | Security | OWASP Top 10 compliance | Bảo vệ dữ liệu nhân sự nhạy cảm |
| NFR-07 | Scalability | Horizontal scale API layer | Tăng trưởng doanh nghiệp |
| NFR-08 | Maintainability | Clean Architecture, SOLID | Dễ mở rộng, dễ test |

### 1.3 Capacity Estimation

**Giả định**: Doanh nghiệp 500 nhân viên

| Metric | Ước tính | Ghi chú |
|--------|---------|---------|
| Daily Active Users | ~200 | 40% NV dùng hàng ngày |
| Peak Concurrent | ~50 | Giờ chấm công sáng 8:00-8:30 |
| Attendance Logs/day | ~1000 | 500 NV × 2 (in/out) |
| Leave Requests/month | ~50 | ~10% NV xin phép/tháng |
| Payroll Records/month | ~500 | 1 per NV |
| API Requests/day | ~10,000 | CRUD + dashboard + reports |
| Storage/year | ~5 GB | MongoDB documents |
| Bandwidth | ~100 MB/day | API JSON responses |

---

## 2. HIGH-LEVEL DESIGN (HLD)

### 2.1 System Architecture

**Kiểu kiến trúc**: Modular Monolith với Event-Driven patterns

**Lý do chọn Modular Monolith thay vì Microservices**:
- Quy mô SME (10–500 NV), không cần scale từng service riêng
- Team nhỏ, dễ maintain 1 codebase
- Strong consistency dễ đảm bảo hơn (đặc biệt payroll)
- Có thể tách thành Microservices sau khi scale lên

**Kiểu kiến trúc phần mềm**: Clean Architecture (4 layers)
- Domain layer (innermost): Entities, Value Objects — không phụ thuộc gì
- Application layer: Business logic, CQRS (Commands/Queries), Events
- Infrastructure layer: Database access, External services
- Presentation layer (outermost): API endpoints

### 2.2 Component Diagram

```
                         ┌─────────────┐
                         │   Clients   │
                         │ Angular SPA │
                         └──────┬──────┘
                                │ HTTPS
                         ┌──────▼──────┐
                         │ API Gateway │
                         │   (YARP/    │
                         │  Nginx)     │
                         └──────┬──────┘
                                │
                    ┌───────────▼───────────┐
                    │    .NET API Server     │
                    │   (Carter Minimal)     │
                    │                       │
                    │  ┌─────────────────┐  │
                    │  │  Auth Module    │  │
                    │  │  HR Module      │  │
                    │  │  Attendance Mod │  │
                    │  │  Leave Module   │  │
                    │  │  Payroll Module │  │
                    │  │  Org Module     │  │
                    │  └─────────────────┘  │
                    │                       │
                    │  ┌─────────────────┐  │
                    │  │ MediatR Bus     │  │
                    │  │ (CQRS + Events) │  │
                    │  └─────────────────┘  │
                    │                       │
                    │  ┌─────────────────┐  │
                    │  │ Background Jobs │  │
                    │  │ - LeaveAccrual  │  │
                    │  │ - AttendanceProc│  │
                    │  └─────────────────┘  │
                    └───┬──────────┬────────┘
                        │          │
               ┌────────▼──┐  ┌───▼────────┐
               │  MongoDB   │  │   Redis    │
               │  (Primary) │  │  (Cache)   │
               │  Port:27017│  │  Port:6379 │
               └────────────┘  └────────────┘
```

### 2.3 Data Flow — Request Pipeline

```
Client Request
    │
    ▼
JWT Authentication Middleware
    │ (Validate token, extract claims)
    ▼
Authorization Check
    │ (Role-based: Admin/HR/Manager/Employee)
    ▼
Validation Filter (FluentValidation)
    │ (Validate DTO fields, business constraints)
    ▼
Carter Endpoint Handler
    │ (Map DTO → Command/Query)
    ▼
MediatR Pipeline
    │ (Dispatch to Handler)
    ▼
Application Service / Command Handler
    │ (Business logic, validation)
    ▼
Repository Layer
    │ (MongoDB CRUD operations)
    ▼
Response → Client
```

### 2.4 Event-Driven Flow

```
CreateEmployeeHandler
    │ publishes
    ▼
EmployeeCreatedEvent ──────────► CreateUserEventHandler
                                     │ (auto create user account)

ContractService.CreateAsync
    │ publishes
    ▼
ContractCreatedEvent ──────────► InitializeLeaveOnContractHandler
                                     │ (init leave allocations)

DeleteEmployeeHandler
    │ publishes
    ▼
EmployeeDeletedEvent ──────────► [Future: CleanupHandler]
```

---

## 3. DATABASE DESIGN

### 3.1 Database Choice: MongoDB (NoSQL)

**Lý do chọn MongoDB**:

| Tiêu chí | MongoDB | SQL (PostgreSQL) | Verdict |
|----------|---------|-----------------|---------|
| Schema flexibility | ✅ Flexible | ❌ Rigid | Employee data thay đổi thường xuyên |
| Embedded documents | ✅ Native | ❌ Cần JOIN | PersonalInfo, BankDetails, SalaryComponents |
| Hierarchical data | ✅ Tốt | ⚠️ CTE needed | Department/Position tree |
| Bucket pattern | ✅ Native | ❌ Khó | AttendanceBucket (1 doc/emp/month) |
| Transaction | ✅ Multi-doc | ✅ ACID | Unit of Work cho payroll/leave |
| Horizontal scale | ✅ Sharding | ⚠️ Complex | Tương lai scale |
| .NET Identity | ✅ Có adapter | ✅ Native | Cả hai đều support |

### 3.2 Collections Overview

| Collection | Document Size (avg) | Growth Rate | Index Strategy |
|-----------|-------------------|-------------|---------------|
| users | 1 KB | Slow | UserName, Email, EmployeeId |
| employees | 2 KB | Slow | EmployeeCode (unique), DepartmentId, PositionId |
| contracts | 1.5 KB | Medium | EmployeeId + Status, StartDate-EndDate |
| departments | 0.5 KB | Rare | Code, ParentId |
| positions | 0.5 KB | Rare | Code, ParentId |
| shifts | 0.5 KB | Rare | Code, IsActive |
| attendance_buckets | 5-15 KB | Monthly | EmployeeId + Month (compound unique) |
| leave_types | 0.5 KB | Rare | Code, IsActive |
| leave_allocations | 0.5 KB | Yearly | EmployeeId + LeaveTypeId + Year (compound) |
| leave_requests | 1 KB | Medium | EmployeeId + Status, FromDate-ToDate |
| payrolls | 3 KB | Monthly | EmployeeId + Month + Year (compound) |
| audit_logs | 2 KB | Fast | TableName + RecordId, CreatedAt |
| system_settings | 0.3 KB | Rare | Key (unique), Group |

### 3.3 Storage Estimation (500 NV, 5 năm)

| Collection | Records/year | 5-year total | Size |
|-----------|-------------|-------------|------|
| employees | 500 | 500 | 1 MB |
| contracts | 750 | 3,750 | 5.6 MB |
| attendance_buckets | 6,000 | 30,000 | 300 MB |
| leave_requests | 600 | 3,000 | 3 MB |
| payrolls | 6,000 | 30,000 | 90 MB |
| audit_logs | 50,000 | 250,000 | 500 MB |
| **TOTAL** | | | **~900 MB** |

### 3.4 Indexing Strategy

**Primary Indexes** (must have):
- `employees`: `{ EmployeeCode: 1 }` (unique)
- `attendance_buckets`: `{ EmployeeId: 1, Month: 1 }` (compound unique)
- `payrolls`: `{ EmployeeId: 1, Month: 1, Year: 1 }` (compound unique)
- `leave_allocations`: `{ EmployeeId: 1, LeaveTypeId: 1, Year: 1 }` (compound)

**Secondary Indexes** (for queries):
- `employees`: `{ "JobDetails.DepartmentId": 1 }`, `{ "JobDetails.Status": 1 }`
- `contracts`: `{ EmployeeId: 1, Status: 1 }`
- `leave_requests`: `{ EmployeeId: 1, Status: 1 }`, `{ FromDate: 1, ToDate: 1 }`
- `audit_logs`: `{ CreatedAt: -1 }` (TTL index for auto-cleanup)

### 3.5 Data Patterns

**Bucket Pattern** (Attendance):
- 1 document = 1 employee × 1 month
- Contains: List of DailyLog (max 31 entries)
- Benefit: Giảm số documents, query 1 tháng = 1 read

**Embedded Document Pattern** (Employee):
- PersonalInfo, JobDetails, BankDetails embedded trong Employee
- Benefit: 1 read lấy đủ thông tin, không cần JOIN

**Snapshot Pattern** (Payroll):
- EmployeeSnapshot lưu thông tin NV tại thời điểm tính lương
- Benefit: Historical data integrity, không bị ảnh hưởng bởi update sau

---

## 4. CACHING STRATEGY

### 4.1 Cache Layer: Redis

| Cache Key | Data | TTL | Invalidation |
|----------|------|-----|-------------|
| `DEPARTMENT_TREE` | Department tree JSON | 1 giờ | On CRUD Department |
| `POSITION_TREE` | Position tree JSON | 1 giờ | On CRUD Position |
| `SYSTEM_SETTINGS:{group}` | Settings by group | 30 phút | On update setting |
| `EMPLOYEE_LOOKUP` | Employee ID-Name map | 15 phút | On CRUD Employee |

### 4.2 Cache Strategy: Cache-Aside

```
1. Client request → Check Redis
2. Cache HIT → Return cached data
3. Cache MISS → Query MongoDB → Store in Redis → Return
4. On Write → Invalidate cache key
```

### 4.3 What NOT to Cache

- Payroll data (cần real-time accuracy)
- Leave balances (thay đổi thường xuyên)
- Attendance logs (write-heavy)
- User authentication tokens (handled by JWT)

---

## 5. SCALABILITY

### 5.1 Current Architecture (Single Server)

Phù hợp cho: 10–200 NV, 1 server

```
[Angular :4200] → [.NET API :5000] → [MongoDB :27017]
                                   → [Redis :6379]
```

### 5.2 Scale-Up Phase (200–500 NV)

**Vertical Scaling**:
- Tăng RAM/CPU cho Docker containers
- MongoDB: Tăng WiredTiger cache
- Redis: Tăng maxmemory

### 5.3 Scale-Out Phase (500–2000 NV)

**Horizontal Scaling**:

```
                    ┌─────────────┐
                    │ Load Balancer│
                    │   (Nginx)   │
                    └──┬───────┬──┘
                       │       │
              ┌────────▼─┐ ┌──▼────────┐
              │ API #1   │ │ API #2    │
              │ (.NET)   │ │ (.NET)    │
              └────┬─────┘ └─────┬─────┘
                   │             │
         ┌─────────▼─────────────▼─────────┐
         │        MongoDB Replica Set       │
         │  Primary → Secondary → Secondary │
         └──────────────┬──────────────────┘
                        │
         ┌──────────────▼──────────────────┐
         │        Redis (Shared)            │
         └─────────────────────────────────┘
```

**Key Considerations**:
- API servers stateless (JWT, no session)
- Redis shared giữa API instances
- MongoDB Replica Set: Primary cho writes, Secondary cho reads
- Background jobs: chỉ chạy trên 1 instance (distributed lock)

### 5.4 Bottleneck Analysis

| Component | Bottleneck | Solution |
|-----------|-----------|---------|
| API Server | CPU-bound payroll calculation | Horizontal scale + async processing |
| MongoDB | Write contention (attendance peak) | Write concern tuning, Replica Set |
| Redis | Memory limit | Eviction policy (allkeys-lru) |
| Background Jobs | Duplicate execution | Distributed lock (Redis SETNX) |
| File Upload | Storage limit | External storage (S3/MinIO) |

---

## 6. SECURITY DESIGN

### 6.1 Authentication Flow

```
Client                    API Server               MongoDB
  │                          │                        │
  │── POST /login ──────────►│                        │
  │   {username, password}   │── FindByName/Email ───►│
  │                          │◄── User document ──────│
  │                          │                        │
  │                          │── CheckPassword ──────►│
  │                          │   (BCrypt verify)      │
  │                          │                        │
  │                          │── Generate JWT ────────│
  │                          │   Claims: UserId,      │
  │                          │   EmployeeId, Roles    │
  │                          │   Expiry: configurable │
  │◄── { token, user } ─────│                        │
  │                          │                        │
  │── GET /api/xxx ─────────►│                        │
  │   Header: Bearer <jwt>   │── Validate JWT ───────│
  │                          │── Check Role ──────────│
  │                          │── Process request ────►│
  │◄── Response ─────────────│                        │
```

### 6.2 Authorization Matrix

**4 Layers of Security**:
1. **Transport**: HTTPS (TLS 1.3)
2. **Authentication**: JWT Bearer Token validation
3. **Authorization**: Role-based access per endpoint
4. **Data-level**: BankDetails hidden for non-Admin/HR/owner

### 6.3 Security Measures

| Category | Measure | Status |
|---------|---------|--------|
| Auth | JWT Bearer Token | ✅ |
| Auth | Password hashing (BCrypt via Identity) | ✅ |
| Auth | Role-based endpoint authorization | ✅ |
| Auth | Account lock/disable capability | ✅ |
| Input | FluentValidation trên tất cả DTOs | ✅ |
| Input | SQL/NoSQL injection prevention (MongoDB driver) | ✅ |
| Transport | CORS configuration | ✅ |
| Data | Soft delete (không xóa vĩnh viễn) | ✅ |
| Data | BankDetails access control | ✅ |
| Data | Audit logging | ✅ (Contract module) |
| Missing | Rate limiting | ❌ Cần thêm |
| Missing | Refresh token | ❌ Cần thêm |
| Missing | Token blacklist (logout) | ❌ Cần thêm |
| Missing | Data encryption at rest | ❌ Cần thêm |
| Missing | Request/Response logging | ❌ Cần thêm |

### 6.4 Data Sensitivity Classification

| Level | Data | Protection |
|-------|------|-----------|
| 🔴 Critical | Password, JWT Secret | Hashed/Config secret |
| 🟠 Sensitive | BankDetails, Salary, IdNumber | Role-based access |
| 🟡 Internal | Employee info, Attendance | Authenticated access |
| 🟢 Public | Department/Position tree | Authenticated access |

---

## 7. RELIABILITY & FAULT TOLERANCE

### 7.1 Failure Scenarios

| Scenario | Impact | Mitigation |
|---------|--------|-----------|
| API Server crash | All services down | Docker auto-restart, health check |
| MongoDB crash | Data unavailable | Replica Set (auto-failover) |
| Redis crash | Cache miss, slower queries | Graceful degradation (fallback to DB) |
| Network partition | API ↔ DB disconnected | Retry policy, circuit breaker |
| Background job failure | Leave accrual missed | Idempotent design, retry logic |
| Concurrent write conflict | Data inconsistency | Optimistic concurrency (Version field) |

### 7.2 Data Integrity Patterns

| Pattern | Implementation | Module |
|---------|---------------|--------|
| Unit of Work | MongoDB Session/Transaction | Employee, Leave, Payroll |
| Optimistic Concurrency | Version field in BaseEntity | All entities |
| Idempotent Operations | LastAccrualMonth check | Leave Accrual |
| Soft Delete | IsDeleted flag | All entities |
| Immutable Snapshots | EmployeeSnapshot in Payroll | Payroll |
| Event-Driven Side Effects | MediatR INotification | Employee → User, Contract → Leave |

### 7.3 Consistency Model

- **Strong Consistency**: Payroll, Leave Balance (financial data)
  - Achieved via: MongoDB transactions (Unit of Work)
- **Eventual Consistency**: Cache (Department/Position trees)
  - TTL-based invalidation, acceptable 1-hour stale data

---

## 8. LOW-LEVEL DESIGN (LLD)

### 8.1 Design Patterns Used

| Pattern | Where | Purpose |
|---------|-------|---------|
| **CQRS** | Application layer | Tách read/write logic, MediatR handlers |
| **Repository** | Infrastructure | Abstract database access |
| **Unit of Work** | Infrastructure | Transaction management |
| **Mediator** | Application | Decouple handlers, MediatR |
| **Observer/Event** | Application | Domain events (Created, Deleted) |
| **Strategy** | Attendance | AttendanceCalculator (pure logic) |
| **Facade** | API Endpoints | Carter modules wrap complex logic |
| **Builder** | Organization | Department/Position tree builder |
| **Snapshot** | Payroll | EmployeeSnapshot for immutability |
| **Bucket** | Attendance | 1 document per employee per month |

### 8.2 CQRS Pattern Detail

```
WRITE SIDE (Commands):
  CreateEmployeeCommand → CreateEmployeeHandler → EmployeeRepository
  ReviewLeaveRequestCommand → ReviewLeaveRequestHandler → LeaveRequestService
  GeneratePayrollCommand → PayrollProcessingService → PayrollRepository

READ SIDE (Queries):
  GetUsersQuery → GetUsersQueryHandler → UserManager
  GetEmployeeById → EmployeeService → EmployeeRepository

EVENTS (Side Effects):
  EmployeeCreatedEvent → CreateUserEventHandler
  ContractCreatedEvent → InitializeLeaveOnContractHandler
```

### 8.3 Dependency Injection Map

```
API Layer (Presentation):
  └── Carter Modules (auto-discovered)
  └── Middleware Pipeline
  └── Swagger + JWT Config

Application Layer:
  └── MediatR (auto-scan Commands/Queries/Events)
  └── FluentValidation (auto-scan Validators)
  └── Services:
      ├── EmployeeService, ContractService
      ├── AttendanceService, AttendanceProcessingService
      ├── LeaveRequestService, LeaveAllocationService
      ├── PayrollService, PayrollProcessingService
      ├── DepartmentService, PositionService
      └── AuditService, FileService

Infrastructure Layer:
  └── Repositories (all registered as Scoped)
  └── MongoContext (Singleton)
  └── CacheService → Redis (Singleton)
  └── TokenService → JWT generation
  └── Background Services (Singleton):
      ├── LeaveAccrualBackgroundService
      └── AttendanceProcessingBackgroundService
```

### 8.4 Key Class Relationships

```
BaseEntity (abstract)
    ├── EmployeeEntity
    │     ├── PersonalInfo (VO)
    │     ├── JobDetails (VO)
    │     └── BankDetails (VO)
    ├── ContractEntity
    │     └── SalaryComponents (VO)
    ├── AttendanceBucket
    │     └── List<DailyLog> (VO)
    ├── Shift
    ├── LeaveType
    ├── LeaveAllocation
    ├── LeaveRequest
    ├── PayrollEntity
    │     └── EmployeeSnapshot (VO)
    ├── Department
    ├── Position
    │     └── SalaryRange (VO)
    ├── AuditLog
    └── SystemSetting
```

---

## 9. API DESIGN PRINCIPLES

### 9.1 RESTful Conventions

| Convention | Example |
|-----------|---------|
| Resource naming | `/api/employees`, `/api/leaves` |
| GET for reads | `GET /api/employees/{id}` |
| POST for creates | `POST /api/employees` |
| PUT for updates | `PUT /api/employees/{id}` |
| DELETE for removes | `DELETE /api/employees/{id}` |
| Nested resources | `GET /api/contracts/employee/{empId}` |
| Action endpoints | `PUT /api/leaves/{id}/review` |
| Pagination | `POST /list` with body `{ page, pageSize }` |

### 9.2 Error Handling Strategy

| HTTP Code | Usage | Exception Type |
|-----------|-------|---------------|
| 200 | Success | — |
| 201 | Created | — |
| 400 | Validation error | ValidationException |
| 401 | Unauthorized | UnauthorizedException |
| 403 | Forbidden (wrong role) | ForbiddenException |
| 404 | Resource not found | NotFoundException |
| 409 | Conflict (duplicate) | ConflictException |
| 500 | Server error | Unhandled Exception |

### 9.3 Response Format

```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully."
}
```

---

## 10. DEPLOYMENT ARCHITECTURE

### 10.1 Docker Compose (Current)

| Service | Image | Port | Resources |
|---------|-------|------|-----------|
| mongodb | mongo:latest | 27017 | 1GB RAM, 10GB disk |
| employee-api | Custom .NET 8 | 5000→8080 | 512MB RAM |
| redis | redis:alpine | 6379 | 256MB RAM |

### 10.2 Production Deployment (Recommended)

```
┌─────────────────────────────────────────┐
│              Docker Compose              │
│                                         │
│  ┌──────────┐  ┌──────────┐            │
│  │ Nginx    │  │ .NET API  │ ×2         │
│  │ (Reverse │──│ (8080)    │ instances  │
│  │  Proxy)  │  └─────┬────┘            │
│  │ :80/:443 │        │                  │
│  └──────────┘  ┌─────▼────┐            │
│                │ MongoDB   │            │
│                │ Replica   │            │
│                │ Set ×3    │            │
│                └─────┬────┘            │
│                ┌─────▼────┐            │
│                │ Redis     │            │
│                │ :6379     │            │
│                └──────────┘            │
│                                         │
│  ┌──────────┐                          │
│  │ Angular  │  (Static files via Nginx) │
│  │ Build    │                          │
│  └──────────┘                          │
└─────────────────────────────────────────┘
```

### 10.3 Environment Configuration

| Variable | Dev | Production |
|---------|-----|-----------|
| JWT Secret | short key | 256-bit random key |
| JWT Expiry | 7 days | 1 hour + refresh token |
| MongoDB | Single node | Replica Set (3 nodes) |
| Redis | No password | Password + TLS |
| CORS | Allow all | Specific origins only |
| Logging | Debug | Warning + Error |
| HTTPS | Off | On (TLS 1.3) |

---

## 11. MONITORING & OBSERVABILITY

### 11.1 Recommended Stack

| Layer | Tool | Purpose |
|-------|------|---------|
| Metrics | Prometheus + Grafana | CPU, Memory, Request rate |
| Logging | Serilog → Seq/ELK | Structured logging |
| Tracing | OpenTelemetry | Request tracing across layers |
| Health Check | ASP.NET Health Checks | MongoDB, Redis connectivity |
| Alerting | Grafana Alerts | Downtime, error spike |

### 11.2 Key Metrics to Monitor

| Metric | Alert Threshold |
|--------|----------------|
| API Response Time (P95) | > 1 second |
| Error Rate (5xx) | > 1% |
| MongoDB Connection Pool | > 80% utilization |
| Redis Memory Usage | > 80% maxmemory |
| Background Job Success Rate | < 99% |
| Disk Usage | > 85% |
| JWT Token Failures | Spike detection |

---

## 12. TRADE-OFFS & DECISIONS

| Decision | Chose | Alternative | Why |
|---------|-------|-------------|-----|
| Architecture | Modular Monolith | Microservices | SME scale, small team, strong consistency |
| Database | MongoDB | PostgreSQL | Schema flexibility, embedded docs, bucket pattern |
| Cache | Redis | In-Memory | Shared cache for multi-instance, persistence |
| Auth | JWT | Session-based | Stateless API, horizontal scaling |
| API Style | REST (Minimal API) | gRPC / GraphQL | Simplicity, broad client support |
| CQRS | MediatR (in-process) | Separate read DB | Overkill for current scale |
| Event Bus | MediatR (in-process) | RabbitMQ/Kafka | Single process, no need for distributed events |
| Validation | FluentValidation | Data Annotations | Richer rules, testable |
| ORM | MongoDB.Driver | Entity Framework | Direct driver better for MongoDB |
| Background Job | IHostedService | Hangfire/Quartz | Simple scheduling needs |
