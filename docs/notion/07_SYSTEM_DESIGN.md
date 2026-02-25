# 🏗️ System Design — HRM EmployeeCleanArch

| Thông tin | Chi tiết |
|-----------|----------|
| Phiên bản | 1.0 |
| Ngày tạo | 17/02/2026 |

---

## 1️⃣ Requirements

### Functional Requirements

| # | Requirement | Priority |
|---|------------|----------|
| FR-01 | User auth (login/register) với JWT Bearer Token | P0 |
| FR-02 | Role-based authorization (Admin, HR, Manager, Employee) | P0 |
| FR-03 | CRUD nhân viên (thông tin cá nhân, công việc, ngân hàng) | P0 |
| FR-04 | Quản lý hợp đồng (tạo, cập nhật, chấm dứt, auto-expire) | P0 |
| FR-05 | Chấm công (check-in/out), xử lý log, tính trạng thái | P0 |
| FR-06 | Quản lý ca (bao gồm ca qua đêm, grace period) | P0 |
| FR-07 | Quản lý nghỉ phép (loại phép, cấp phát, xin/duyệt, cộng dồn) | P0 |
| FR-08 | Tính lương tự động (BH, thuế TNCN 7 bậc, OT) | P0 |
| FR-09 | Phòng ban/chức vụ phân cấp (tree, cycle detection) | P1 |
| FR-10 | Sơ đồ tổ chức + Audit log + System settings | P1 |
| FR-11 | Tuyển dụng (tin TD, ứng viên, phỏng vấn) | P2 |

### Non-Functional Requirements

| # | Requirement | Target |
|---|------------|--------|
| NFR-01 | Response Time | < 500ms (P95) |
| NFR-02 | Availability | 99.5% uptime |
| NFR-03 | Concurrent Users | 50–200 |
| NFR-04 | Payroll/Leave Consistency | Strong (transaction) |
| NFR-05 | Data Retention | 7 năm payroll, 3 năm attendance |
| NFR-06 | Security | OWASP Top 10 |
| NFR-07 | Horizontal Scaling | API layer stateless |
| NFR-08 | Maintainability | Clean Architecture, SOLID |

### Capacity Estimation (500 NV)

| Metric | Ước tính |
|--------|---------|
| Daily Active Users | ~200 |
| Peak Concurrent | ~50 (giờ chấm công 8:00-8:30) |
| Attendance Logs/day | ~1000 (500 NV × 2 in/out) |
| API Requests/day | ~10,000 |
| Storage/year | ~5 GB |

---

## 2️⃣ High-Level Design (HLD)

### Kiểu kiến trúc: Modular Monolith

Lý do chọn Modular Monolith thay vì Microservices:
- Quy mô SME (10–500 NV), không cần scale từng service
- Team nhỏ, dễ maintain 1 codebase
- Strong consistency dễ đảm bảo (đặc biệt payroll)
- Có thể tách Microservices sau khi scale lên

### Component Diagram

```
Clients (Angular SPA)
    │ HTTPS
    ▼
API Gateway (Nginx/YARP)
    │
    ▼
.NET API Server (Carter Minimal)
    ├── Auth Module
    ├── HR Module
    ├── Attendance Module
    ├── Leave Module
    ├── Payroll Module
    ├── Organization Module
    ├── MediatR Bus (CQRS + Events)
    └── Background Jobs
            ├── LeaveAccrualService
            └── AttendanceProcessingService
    │               │
    ▼               ▼
MongoDB         Redis
(Primary DB)    (Cache)
```

### Request Pipeline

1. Client Request
2. JWT Authentication Middleware (validate token, extract claims)
3. Authorization Check (role-based)
4. Validation Filter (FluentValidation)
5. Carter Endpoint Handler (map DTO → Command/Query)
6. MediatR Pipeline (dispatch to Handler)
7. Application Service / Command Handler (business logic)
8. Repository Layer (MongoDB CRUD)
9. Response → Client

### Event-Driven Flow

- **EmployeeCreatedEvent** → CreateUserEventHandler (auto tạo user)
- **ContractCreatedEvent** → InitializeLeaveOnContractHandler (init leave)
- **EmployeeDeletedEvent** → [Future: CleanupHandler]

---

## 3️⃣ Database Design

### Tại sao chọn MongoDB?

| Tiêu chí | MongoDB | SQL |
|----------|---------|-----|
| Schema flexibility | ✅ Flexible | ❌ Rigid |
| Embedded documents | ✅ Native | ❌ Cần JOIN |
| Hierarchical data | ✅ Tốt | ⚠️ CTE needed |
| Bucket pattern | ✅ Native | ❌ Khó |
| Transaction | ✅ Multi-doc | ✅ ACID |

### Collections

| Collection | Avg Size | Growth | Key Indexes |
|-----------|---------|--------|-------------|
| users | 1 KB | Slow | UserName, Email |
| employees | 2 KB | Slow | EmployeeCode (unique) |
| contracts | 1.5 KB | Medium | EmployeeId + Status |
| departments | 0.5 KB | Rare | Code, ParentId |
| positions | 0.5 KB | Rare | Code, ParentId |
| shifts | 0.5 KB | Rare | Code |
| attendance_buckets | 5-15 KB | Monthly | EmployeeId + Month (compound unique) |
| leave_types | 0.5 KB | Rare | Code |
| leave_allocations | 0.5 KB | Yearly | EmployeeId + LeaveTypeId + Year |
| leave_requests | 1 KB | Medium | EmployeeId + Status |
| payrolls | 3 KB | Monthly | EmployeeId + Month + Year |
| audit_logs | 2 KB | Fast | TableName + RecordId |
| system_settings | 0.3 KB | Rare | Key (unique) |

### Storage Estimation (500 NV × 5 năm)

| Collection | 5-year total | Size |
|-----------|-------------|------|
| attendance_buckets | 30,000 docs | 300 MB |
| payrolls | 30,000 docs | 90 MB |
| audit_logs | 250,000 docs | 500 MB |
| Others | ~10,000 docs | ~10 MB |
| **TOTAL** | | **~900 MB** |

### Data Patterns

- **Bucket Pattern** (Attendance): 1 document = 1 NV × 1 tháng → giảm reads
- **Embedded Document** (Employee): PersonalInfo, BankDetails embedded → no JOIN
- **Snapshot Pattern** (Payroll): EmployeeSnapshot immutable → historical accuracy

---

## 4️⃣ Caching Strategy (Redis)

### Cache Map

| Cache Key | Data | TTL | Invalidation |
|----------|------|-----|-------------|
| DEPARTMENT_TREE | Department tree JSON | 1 giờ | On CRUD |
| POSITION_TREE | Position tree JSON | 1 giờ | On CRUD |
| SYSTEM_SETTINGS:{group} | Settings by group | 30 phút | On update |
| EMPLOYEE_LOOKUP | ID-Name map | 15 phút | On CRUD |

### Strategy: Cache-Aside

1. Check Redis → HIT → Return
2. MISS → Query MongoDB → Store Redis → Return
3. On Write → Invalidate key

### Do NOT Cache

- Payroll data (real-time accuracy)
- Leave balances (thay đổi thường xuyên)
- Attendance logs (write-heavy)

---

## 5️⃣ Scalability

### Phase 1: Single Server (10-200 NV)

Angular → .NET API → MongoDB + Redis (all on 1 server/Docker)

### Phase 2: Vertical Scale (200-500 NV)

Tăng RAM/CPU cho Docker containers

### Phase 3: Horizontal Scale (500-2000 NV)

```
Load Balancer (Nginx)
    ├── API Instance #1
    └── API Instance #2
            │
    MongoDB Replica Set (Primary + 2 Secondary)
            │
    Redis (Shared)
```

Điều kiện:
- API stateless (JWT, no session)
- Redis shared giữa instances
- MongoDB Replica Set: Primary writes, Secondary reads
- Background jobs: distributed lock (1 instance chạy)

### Bottleneck Analysis

| Component | Bottleneck | Solution |
|-----------|-----------|---------|
| API | CPU payroll calculation | Horizontal scale + async |
| MongoDB | Write contention (attendance peak) | Replica Set |
| Redis | Memory limit | Eviction policy (allkeys-lru) |
| Background Jobs | Duplicate execution | Redis SETNX lock |
| File Upload | Storage | External S3/MinIO |

---

## 6️⃣ Security Design

### Authentication Flow

1. POST /login (username + password)
2. Server: FindByName/Email → CheckPassword (BCrypt)
3. Server: Generate JWT (UserId, EmployeeId, Roles, Expiry)
4. Client: Store token → Send as Bearer header
5. Server: Validate JWT → Check Role → Process request

### 4 Layers of Security

1. **Transport**: HTTPS (TLS 1.3)
2. **Authentication**: JWT Bearer Token validation
3. **Authorization**: Role-based access per endpoint
4. **Data-level**: BankDetails hidden for non-Admin/HR/owner

### Security Checklist

| Measure | Status |
|---------|--------|
| JWT Bearer Token | ✅ |
| Password Hashing (BCrypt) | ✅ |
| Role-based Authorization | ✅ |
| Account Lock/Disable | ✅ |
| FluentValidation (all DTOs) | ✅ |
| CORS Configuration | ✅ |
| Soft Delete | ✅ |
| BankDetails Access Control | ✅ |
| Audit Logging | ✅ |
| Rate Limiting | ❌ Cần thêm |
| Refresh Token | ❌ Cần thêm |
| Token Blacklist (logout) | ❌ Cần thêm |
| Data Encryption at Rest | ❌ Cần thêm |

### Data Sensitivity

| Level | Data |
|-------|------|
| 🔴 Critical | Password, JWT Secret |
| 🟠 Sensitive | BankDetails, Salary, IdNumber |
| 🟡 Internal | Employee info, Attendance |
| 🟢 Public | Department/Position tree |

---

## 7️⃣ Reliability & Fault Tolerance

### Failure Scenarios

| Scenario | Mitigation |
|---------|-----------|
| API crash | Docker auto-restart, health check |
| MongoDB crash | Replica Set auto-failover |
| Redis crash | Graceful degradation (fallback to DB) |
| Network partition | Retry policy, circuit breaker |
| Background job fail | Idempotent design, retry |
| Concurrent write | Optimistic concurrency (Version field) |

### Data Integrity Patterns

| Pattern | Module |
|---------|--------|
| Unit of Work (MongoDB Transaction) | Employee, Leave, Payroll |
| Optimistic Concurrency (Version) | All entities |
| Idempotent (LastAccrualMonth) | Leave Accrual |
| Soft Delete (IsDeleted) | All entities |
| Immutable Snapshot | Payroll |
| Event-Driven Side Effects | Employee → User, Contract → Leave |

### Consistency Model

- **Strong**: Payroll, Leave Balance → MongoDB transactions
- **Eventual**: Cache trees → TTL 1 giờ, acceptable stale

---

## 8️⃣ Low-Level Design (LLD)

### Design Patterns

| Pattern | Where | Purpose |
|---------|-------|---------|
| CQRS | Application | Tách read/write, MediatR |
| Repository | Infrastructure | Abstract DB access |
| Unit of Work | Infrastructure | Transaction |
| Mediator | Application | Decouple handlers |
| Observer/Event | Application | Domain events |
| Strategy | Attendance | AttendanceCalculator |
| Facade | API | Carter modules |
| Builder | Organization | Tree builder |
| Snapshot | Payroll | Immutable employee data |
| Bucket | Attendance | 1 doc/emp/month |

### CQRS Flow

**Write Side** (Commands):
- CreateEmployeeCommand → Handler → Repository
- ReviewLeaveRequestCommand → Handler → Service
- GeneratePayrollCommand → PayrollProcessingService

**Read Side** (Queries):
- GetUsersQuery → Handler → UserManager
- GetEmployeeById → Service → Repository

**Events** (Side Effects):
- EmployeeCreatedEvent → CreateUserEventHandler
- ContractCreatedEvent → InitializeLeaveOnContractHandler

### Dependency Injection

**API Layer**: Carter Modules, Middleware, Swagger, JWT

**Application Layer**: MediatR, FluentValidation, Services (Employee, Contract, Attendance, Leave, Payroll, Department, Position, Audit, File)

**Infrastructure Layer**: Repositories (Scoped), MongoContext (Singleton), CacheService/Redis (Singleton), TokenService, Background Services (Singleton)

### Entity Hierarchy

```
BaseEntity (abstract)
├── EmployeeEntity (+PersonalInfo, +JobDetails, +BankDetails)
├── ContractEntity (+SalaryComponents)
├── AttendanceBucket (+List<DailyLog>)
├── Shift
├── LeaveType / LeaveAllocation / LeaveRequest
├── PayrollEntity (+EmployeeSnapshot)
├── Department / Position (+SalaryRange, +DepartmentId)
├── AuditLog / SystemSetting
```

---

## 9️⃣ API Design

### RESTful Conventions

| Convention | Example |
|-----------|---------|
| Resource naming | /api/employees, /api/leaves |
| GET reads | GET /api/employees/{id} |
| POST creates | POST /api/employees |
| PUT updates | PUT /api/employees/{id} |
| DELETE removes | DELETE /api/employees/{id} |
| Actions | PUT /api/leaves/{id}/review |
| Pagination | POST /list { page, pageSize } |

### Error Codes

| Code | Usage |
|------|-------|
| 200 | Success |
| 400 | Validation error |
| 401 | Unauthorized |
| 403 | Forbidden (wrong role) |
| 404 | Not found |
| 409 | Conflict (duplicate) |
| 500 | Server error |

### Response Format

```json
{ "success": true, "data": {}, "message": "..." }
```

---

## 🔟 Deployment

### Docker Compose (Current)

| Service | Port | Resources |
|---------|------|-----------|
| mongodb | 27017 | 1GB RAM, 10GB disk |
| employee-api | 5000→8080 | 512MB RAM |
| redis | 6379 | 256MB RAM |

### Production Config

| Variable | Dev | Production |
|---------|-----|-----------|
| JWT Secret | short key | 256-bit random |
| JWT Expiry | 7 days | 1 hour + refresh |
| MongoDB | Single node | Replica Set ×3 |
| Redis | No password | Password + TLS |
| CORS | Allow all | Specific origins |
| Logging | Debug | Warning + Error |
| HTTPS | Off | TLS 1.3 |

---

## 1️⃣1️⃣ Monitoring

### Recommended Stack

| Layer | Tool |
|-------|------|
| Metrics | Prometheus + Grafana |
| Logging | Serilog → Seq/ELK |
| Tracing | OpenTelemetry |
| Health Check | ASP.NET Health Checks |

### Alert Thresholds

| Metric | Threshold |
|--------|----------|
| Response Time (P95) | > 1 second |
| Error Rate (5xx) | > 1% |
| MongoDB Connection Pool | > 80% |
| Redis Memory | > 80% |
| Disk Usage | > 85% |

---

## 1️⃣2️⃣ Trade-offs & Decisions

| Chose | Over | Why |
|-------|------|-----|
| Modular Monolith | Microservices | SME scale, small team |
| MongoDB | PostgreSQL | Schema flexibility, embedded docs |
| Redis | In-Memory cache | Shared cache, multi-instance |
| JWT | Session-based | Stateless, horizontal scaling |
| REST Minimal API | gRPC/GraphQL | Simplicity |
| MediatR CQRS | Separate read DB | Current scale sufficient |
| MediatR Events | RabbitMQ/Kafka | Single process |
| FluentValidation | Data Annotations | Richer, testable |
| IHostedService | Hangfire/Quartz | Simple needs |
