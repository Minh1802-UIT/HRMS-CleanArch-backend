# Module Flow Diagrams — HRMS

> **Document Version:** 1.0  
> **Last Updated:** March 6, 2026  
> **Author:** Senior Developer  
> **Tool:** Mermaid (sequence diagrams, flowcharts, state machines)

---

## Table of Contents

1. [Authentication Flows](#1-authentication-flows)
   - 1.1 Login
   - 1.2 Refresh Token (với Reuse Detection)
   - 1.3 Forgot / Reset Password
   - 1.4 Change Password
2. [Employee Onboarding Flow](#2-employee-onboarding-flow)
   - 2.1 Tạo nhân viên trực tiếp
   - 2.2 Account Provisioning (Event-driven)
3. [Leave Request Lifecycle](#3-leave-request-lifecycle)
   - 3.1 Submit Leave Request
   - 3.2 Review (Approve / Reject)
   - 3.3 State Machine
4. [Attendance Processing Pipeline](#4-attendance-processing-pipeline)
   - 4.1 Check-in / Check-out
   - 4.2 Raw Log → Attendance Bucket (Background Processing)
5. [Payroll Generation Flow](#5-payroll-generation-flow)
   - 5.1 Tổng quan quy trình
   - 5.2 Công thức tính lương
   - 5.3 Vòng đời trạng thái bảng lương
6. [Recruitment Pipeline](#6-recruitment-pipeline)
   - 6.1 Từ Vacancy đến Onboarding
   - 6.2 Onboard Candidate → Employee
   - 6.3 State Machine ứng viên
7. [Background Services Schedule](#7-background-services-schedule)
8. [Domain Event Bus](#8-domain-event-bus)
9. [Request Pipeline (Middleware / CQRS)](#9-request-pipeline-middleware--cqrs)

---

## 1. Authentication Flows

### 1.1 Login

**Mô tả:** Người dùng đăng nhập bằng `username/email + password`. Server kiểm tra nhiều lớp bảo vệ (lockout, isActive, password) trước khi phát hành token. Refresh token được lưu trong `httpOnly cookie`, **không** trả về body để tránh XSS.

```mermaid
sequenceDiagram
    participant FE as Angular Frontend
    participant API as /api/auth/login
    participant IS as IdentityService
    participant DB as MongoDB (users)

    FE->>API: POST /login { username, password }
    API->>IS: LoginAsync(username, password)
    IS->>DB: FindByName(username) or FindByEmail(username)
    DB-->>IS: ApplicationUser | null

    alt User not found
        IS-->>API: throw UnauthorizedException("Account not found.")
        API-->>FE: 401 Unauthorized
    else Account locked (LockoutEnd > now)
        IS-->>API: throw UnauthorizedException("Account is locked.")
        API-->>FE: 401 Unauthorized
    else IsActive = false
        IS-->>API: throw UnauthorizedException("Account is disabled.")
        API-->>FE: 401 Unauthorized
    else Wrong password
        IS-->>API: throw UnauthorizedException("Invalid password.")
        API-->>FE: 401 Unauthorized
    else All checks passed
        IS->>IS: GetRolesAsync(user)
        IS->>IS: GenerateJwtToken(userId, email, roles, employeeId)
        IS->>IS: GenerateRefreshToken() → raw token
        IS->>IS: Hash(rawToken) → stored hash
        IS->>IS: Revoke ALL existing refresh tokens (single-session policy)
        IS->>IS: PruneRefreshTokens (remove old revoked entries)
        IS->>DB: UpdateAsync(user) — add new RefreshTokenEntry{hash, familyId}
        IS-->>API: LoginResponseDto { accessToken, rawRefreshToken, expiresIn, user }
        API->>FE: Set-Cookie: refreshToken=<raw>; HttpOnly; Secure; SameSite=None (7 days)
        API-->>FE: 200 OK { accessToken, tokenType, expiresIn, user }
    end
```

---

### 1.2 Refresh Token (với Reuse Detection)

**Mô tả:** Access token tồn tại 60 phút (dev) / 30 phút (prod). Khi hết hạn, frontend gọi refresh bằng httpOnly cookie. Hệ thống dùng **token rotation** và **reuse detection** — nếu một token đã dùng được gửi lại, toàn bộ session bị thu hồi (chống token theft).

```mermaid
sequenceDiagram
    participant FE as Angular Frontend
    participant API as /api/auth/refresh-token
    participant IS as IdentityService
    participant DB as MongoDB (users)

    FE->>API: POST /refresh-token { accessToken } + Cookie: refreshToken=<raw>
    API->>IS: RefreshTokenAsync(accessToken, rawRefreshToken)

    alt accessToken is empty (page reload — in-memory token lost)
        IS->>DB: FindUser where tokens contain Hash(rawRefreshToken)
    else accessToken present
        IS->>IS: GetPrincipalFromExpiredToken(accessToken)
        IS->>IS: Extract userId from claims (NameIdentifier)
        IS->>DB: FindByIdAsync(userId)
    end

    IS->>IS: incomingHash = Hash(rawRefreshToken)
    IS->>IS: Find entry where TokenHash == incomingHash

    alt Entry not found (unknown or pruned token)
        IS-->>API: throw UnauthorizedException("Invalid refresh token.")
        API-->>FE: 401 Unauthorized
    else Entry.IsRevoked = true (REUSE DETECTED!)
        IS->>IS: Revoke ENTIRE family (all tokens with same familyId)
        IS->>DB: UpdateAsync(user)
        IS-->>API: throw UnauthorizedException("Token revoked. Session terminated.")
        API-->>FE: 401 Unauthorized — force re-login
    else Entry expired (ExpiresAt < now)
        IS-->>API: throw UnauthorizedException("Refresh token expired.")
        API-->>FE: 401 Unauthorized
    else Valid
        IS->>IS: Mark current entry IsRevoked = true
        IS->>IS: GenerateRefreshToken() → newRawToken
        IS->>IS: Add new RefreshTokenEntry { hash(newRawToken), same familyId }
        IS->>IS: PruneRefreshTokens (revoked > 24h, expired > 30 days)
        IS->>IS: GenerateJwtToken(...) → newAccessToken
        IS->>DB: UpdateAsync(user)
        IS-->>API: LoginResponseDto { newAccessToken, newRawToken }
        API->>FE: Set-Cookie: refreshToken=<newRaw>; HttpOnly; Secure (updated)
        API-->>FE: 200 OK { accessToken, tokenType, expiresIn }
    end
```

---

### 1.3 Forgot / Reset Password

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant API as /api/auth
    participant IS as IdentityService
    participant Email as Email Service

    Note over FE,Email: --- Bước 1: Yêu cầu đặt lại ---
    FE->>API: POST /forgot-password { email }
    API->>IS: GenerateForgotPasswordTokenAsync(email)
    IS->>IS: FindByEmailAsync(email)
    alt User not found
        IS-->>API: throw NotFoundException
        API-->>FE: 404 (hoặc 200 generic để tránh email enumeration)
    else User found
        IS->>IS: GeneratePasswordResetTokenAsync(user)
        IS-->>API: resetToken (raw)
        API->>Email: SendResetEmail(to=email, token=resetToken, link)
        API-->>FE: 200 OK "Reset link sent" (token KHÔNG trả về body)
    end

    Note over FE,Email: --- Bước 2: Xác nhận reset ---
    FE->>API: POST /reset-password { email, token, newPassword, confirmPassword }
    API->>IS: ResetPasswordAsync(email, token, newPassword)
    IS->>IS: FindByEmailAsync(email)
    IS->>IS: ResetPasswordAsync(user, token, newPassword) — ASP.NET Identity validates token
    alt Token invalid/expired
        IS-->>API: Result.Failure(errors)
        API-->>FE: 400 Bad Request
    else Success
        IS-->>API: Result.Success()
        API-->>FE: 200 OK "Password reset successfully."
    end
```

---

### 1.4 Change Password

```mermaid
flowchart TD
    A[POST /api/auth/change-password] --> B{Authenticated?}
    B -- No --> C[401 Unauthorized]
    B -- Yes --> D[ChangePasswordAsync\ncurrentPassword, newPassword]
    D --> E{Current password\ncorrect?}
    E -- No --> F[400 Bad Request\nIdentity errors]
    E -- Yes --> G[Update password hash]
    G --> H{MustChangePassword\n= true?}
    H -- Yes --> I[Set MustChangePassword = false\nClear forced-change flag]
    H -- No --> J[Done]
    I --> J
    J --> K[200 OK]
```

---

## 2. Employee Onboarding Flow

### 2.1 Tạo nhân viên trực tiếp

```mermaid
sequenceDiagram
    participant Admin as Admin / HR
    participant API as POST /api/employees
    participant Handler as CreateEmployeeHandler
    participant Repo as IEmployeeRepository
    participant Pub as IPublisher (MediatR)
    participant EH as CreateUserEventHandler
    participant HF as Hangfire (Background Queue)
    participant IS as IdentityService

    Admin->>API: POST /api/employees { employeeCode, fullName, email, ... }
    API->>Handler: Send(CreateEmployeeCommand)

    Handler->>Repo: ExistsByCodeAsync(employeeCode)
    alt Duplicate code
        Handler-->>API: throw ConflictException
        API-->>Admin: 409 Conflict
    end

    Handler->>Repo: ExistsByEmailAsync(email)
    alt Duplicate email
        Handler-->>API: throw ConflictException
        API-->>Admin: 409 Conflict
    end

    Handler->>Handler: new EmployeeEntity(code, fullName, email)
    Handler->>Handler: employee.UpdatePersonalInfo(...)
    Handler->>Handler: employee.UpdateJobDetails(...)
    Handler->>Handler: employee.UpdateBankDetails(...)
    Handler->>Repo: CreateAsync(employee)
    Repo-->>Handler: saved (employee.Id assigned)

    Handler->>Pub: Publish(EmployeeCreatedEvent { employeeId, email, fullName, phone })

    Note over Pub,HF: Event handlers run in-process (MediatR notification)
    Pub->>EH: Handle(EmployeeCreatedEvent)
    EH->>HF: EnqueueAccountProvisioning(employeeId, email, fullName, phone)
    Note over HF,IS: Hangfire runs async, persisted in MongoDB (retry 5x)
    HF-->>IS: CreateUserAsync(username=email, password=random)
    HF-->>IS: AddToRole("Employee")
    HF-->>IS: SetMustChangePassword = true

    Handler-->>API: EmployeeDto
    API-->>Admin: 201 Created { id, employeeCode, fullName, ... }
```

---

### 2.2 Account Provisioning (Event-driven)

```mermaid
flowchart TD
    A[EmployeeCreatedEvent published] --> B[CreateUserEventHandler]
    B --> C[EnqueueAccountProvisioning\nvia Hangfire]
    C --> D[(MongoDB — Hangfire jobs)]
    D --> E{Hangfire Worker\ntries to execute}
    E -- Success --> F[CreateUserAsync\nusername = email]
    F --> G[AddToRole Employee]
    G --> H[MustChangePassword = true]
    H --> I[Provisioning complete ✅]
    E -- Fail attempt 1-4 --> J[Exponential backoff\n10s, 20s, 40s, 80s]
    J --> E
    E -- Fail attempt 5 --> K[Mark job as Failed\nin Hangfire dashboard]
    K --> L[Admin can retry manually\nvia /hangfire UI]
```

> **Note:** Hangfire persists jobs in MongoDB — nếu server restart giữa chừng, job sẽ được retry tự động khi server khởi động lại.

---

## 3. Leave Request Lifecycle

### 3.1 Submit Leave Request

```mermaid
sequenceDiagram
    participant EMP as Employee
    participant API as POST /api/leaves
    participant Handler as CreateLeaveRequestHandler
    participant TypeRepo as ILeaveTypeRepository
    participant AllocSvc as ILeaveAllocationService
    participant Repo as ILeaveRequestRepository
    participant Pub as IPublisher

    EMP->>API: POST /api/leaves { leaveType, fromDate, toDate, reason }
    API->>Handler: Send(CreateLeaveRequestCommand)

    Note over Handler,TypeRepo: 1. Resolve LeaveType (by enum name or document ID)
    Handler->>TypeRepo: GetByCodeAsync(leaveType)
    alt LeaveType not found
        Handler-->>API: throw NotFoundException
        API-->>EMP: 404 Not Found
    end

    Note over Handler,Repo: 2. Check scheduling overlap
    Handler->>Repo: ExistsOverlapAsync(employeeId, fromDate, toDate)
    alt Overlap exists
        Handler-->>API: throw ConflictException
        API-->>EMP: 409 Conflict "Đã có đơn nghỉ trong khoảng này"
    end

    Note over Handler,AllocSvc: 3. Check balance
    Handler->>Handler: daysRequested = SandwichRule?\n  CountCalendarDays : CountWorkingDays
    Handler->>AllocSvc: GetByEmployeeAndTypeAsync(employeeId, leaveTypeId, year)
    AllocSvc-->>Handler: allocation { remainingDays }
    alt Insufficient balance
        Handler-->>API: throw ValidationException
        API-->>EMP: 400 "Insufficient leave balance"
    end

    Note over Handler,Pub: 4. Create & Save
    Handler->>Handler: new LeaveRequest(employeeId, leaveCategory, fromDate, toDate, reason)
    Handler->>Repo: CreateAsync(entity)
    Note right of Handler: Balance NOT deducted yet!\nDeduction happens only on Approval

    Handler->>Pub: Publish(LeaveRequestSubmittedEvent)
    Note over Pub: Async side effects (in-process)
    Pub->>Pub: LeaveRequestSubmittedEventHandler\n→ AuditLog("SUBMIT_LEAVE_REQUEST")

    Handler-->>API: LeaveRequestDto
    API-->>EMP: 201 Created
```

---

### 3.2 Review (Approve / Reject)

```mermaid
sequenceDiagram
    participant MGR as Manager / HR / Admin
    participant API as POST /api/leaves/{id}/review
    participant Handler as ReviewLeaveRequestHandler
    participant Repo as ILeaveRequestRepository
    participant TypeRepo as ILeaveTypeRepository
    participant AllocSvc as ILeaveAllocationService
    participant Audit as IAuditLogService
    participant Pub as IPublisher

    MGR->>API: POST /leaves/{id}/review { status: "Approved", managerComment }
    API->>Handler: Send(ReviewLeaveRequestCommand)

    Handler->>Repo: GetByIdAsync(id)
    alt Not found
        Handler-->>API: throw NotFoundException
        API-->>MGR: 404
    end

    alt Invalid status value
        Handler-->>API: throw ValidationException
        API-->>MGR: 400
    end

    Note over Handler,TypeRepo: Pre-resolve LeaveType trước khi persist (fail-fast)
    alt Status = Approved
        Handler->>TypeRepo: GetByCodeAsync(entity.LeaveType)
        Handler->>Handler: workingDays = SandwichRule?\n  CalendarDays : WorkingDays
    end

    Note over Handler: Domain Logic
    alt Approve
        Handler->>Handler: entity.Approve(approvedBy, comment)\n→ Status = Approved
    else Reject
        Handler->>Handler: entity.Reject(approvedBy, comment)\n→ Status = Rejected
    end

    Handler->>Repo: UpdateAsync(id, entity, expectedVersion)
    Note right of Repo: Optimistic Concurrency!\nThrow ConcurrencyException if version mismatch

    alt Approved — deduct balance
        Handler->>AllocSvc: UpdateUsedDaysAsync(employeeId, leaveTypeId, year, workingDays)
    end

    Handler->>Audit: LogAsync(REVIEW_LEAVE_REQUEST, oldStatus, newStatus)

    alt Approved
        Handler->>Pub: Publish(LeaveRequestApprovedEvent)
        Note over Pub: LeaveRequestApprovedEventHandler\n→ NotificationService.CreateAsync\n  "Leave Request Approved ✅"
    else Rejected
        Handler->>Pub: Publish(LeaveRequestRejectedEvent)
        Note over Pub: LeaveRequestRejectedEventHandler\n→ NotificationService.CreateAsync\n  "Leave Request Rejected ❌"
    end

    Handler-->>API: void (204)
    API-->>MGR: 200 OK
```

---

### 3.3 State Machine — Leave Request

```mermaid
stateDiagram-v2
    [*] --> Pending : Employee submits\n(balance check passed)

    Pending --> Approved : Manager/HR approves\n(balance deducted)
    Pending --> Rejected : Manager/HR rejects\n(balance unchanged)
    Pending --> Cancelled : Employee cancels

    Approved --> [*]
    Rejected --> [*]
    Cancelled --> [*]

    note right of Pending
        Thời điểm này: balance đã được
        kiểm tra nhưng CHƯA bị trừ.
        Chỉ trừ khi status → Approved.
    end note

    note right of Approved
        Domain Event: LeaveRequestApprovedEvent
        Side effects:
        - Balance -= workingDays
        - Notification to employee
        - AuditLog
    end note
```

---

## 4. Attendance Processing Pipeline

### 4.1 Check-in / Check-out

```mermaid
sequenceDiagram
    participant DEV as Mobile/Web Device
    participant API as POST /api/attendance/check-in
    participant Handler as CheckInHandler
    participant RawRepo as IRawAttendanceLogRepository
    participant ProcSvc as IAttendanceProcessingService

    DEV->>API: POST /attendance/check-in\n{ employeeId, timestamp, deviceId, lat, lng }
    Note over API: Rate limit: 10 events/hour/user (policy: "checkin")
    API->>Handler: Send(CheckInCommand)

    Note over Handler,RawRepo: 1. Spam Protection (60s guard)
    Handler->>RawRepo: GetLatestLogAsync(employeeId)
    RawRepo-->>Handler: latestLog | null
    alt < 60 seconds since last punch
        Handler-->>API: throw ConflictException("Vui lòng chờ X giây nữa")
        API-->>DEV: 409 Conflict
    end

    Note over Handler,RawRepo: 2. Persist raw punch
    Handler->>Handler: dto.ToRawEntity(employeeId) → RawAttendanceLog
    Handler->>RawRepo: CreateAsync(rawLog) — IsProcessed = false

    Note over Handler,ProcSvc: 3. Trigger inline processing (best-effort)
    Handler->>ProcSvc: ProcessRawLogsAsync()
    alt Processing succeeds
        Note over ProcSvc: Attendance bucket updated in real-time
    else Processing fails
        Note over Handler: LogWarning only — never fail check-in!\nBackground job will retry.
    end

    Handler-->>API: void
    API-->>DEV: 200 OK "Check-in recorded successfully."
```

---

### 4.2 Raw Log → Attendance Bucket (Background Processing)

```mermaid
flowchart TD
    A[AttendanceProcessingBackgroundJob\nevery 5 minutes - configurable] --> B[ProcessRawLogsAsync]
    B --> C[GetAndLockUnprocessedLogsAsync\nbatch = 50 logs]
    C --> D{Logs found?}
    D -- No --> E[Sleep until next interval]
    D -- Yes --> F[Load public holidays\nfor dates in batch]
    F --> G[Group by EmployeeId + LogicalDate]
    G --> H[For each group...]

    H --> I[Get Employee → find ShiftId]
    I --> J[Get Shift config\nstartTime, endTime, graceperiod, isOvernight]
    J --> K[Sort punches by timestamp\nApply LogicalDay cutoff at 06:00]

    K --> L{Punch type?}
    L -- CheckIn only --> M[Status = CheckedIn\nrecord checkInTime]
    L -- CheckIn + CheckOut --> N[Calculate hours\nDetect late/early departure]
    N --> O{Is holiday or weekend?}
    O -- Yes --> P[Status = Holiday/DayOff\nactualDays = 0]
    O -- No --> Q[AttendanceCalculator\ncalculate status + overtime]
    Q --> R{Late > graceperiod?}
    R -- Yes --> S[Status = Late\nrecord lateMinutes]
    R -- No --> T[Status = Present]

    M --> U[Upsert AttendanceBucket\nin MongoDB]
    P --> U
    S --> U
    T --> U
    U --> V[Mark raw logs IsProcessed = true]
    V --> H

    H --> W{All groups done?}
    W -- No --> H
    W -- Yes --> X[Log summary: N processed, M failed]
    X --> E

    style A fill:#4a90d9,color:#fff
    style U fill:#27ae60,color:#fff
    style E fill:#95a5a6,color:#fff
```

**Logical Day Rule (BUG-01 fix):** Bất kỳ punch nào có giờ local < 06:00 được tính là thuộc **ngày hôm trước** — để ca đêm (check-out lúc 05:30 AM ngày D+1) vẫn nằm trong cùng logical workday với check-in từ tối ngày D.

---

## 5. Payroll Generation Flow

### 5.1 Tổng quan quy trình

```mermaid
flowchart TD
    subgraph "Bước 1: Chuẩn bị"
        A[Admin/HR tạo Public Holidays\n/api/public-holidays] --> B
        B[Tạo Payroll Cycle\nPOST /api/payroll-cycles/generate\nmonth, year] --> C
        C[(PayrollCycle saved\nstandardWorkingDays = N\npublicHolidaysExcluded = M\nstatus = Open)]
    end

    subgraph "Bước 2: Tính lương"
        D[POST /api/payrolls/generate\nmonth = MM-YYYY] --> E
        E[PayrollProcessingService\nCalculatePayrollAsync] --> F
        F[PayrollDataProvider\nFetchCalculationDataAsync]
        F --> G[Load: Employees, Salary contracts,\nAttendance buckets, Prev payrolls,\nSystem settings, Dept/Position names]
        G --> H[For each employee...]
        H --> I{Has salary\ncontract?}
        I -- No --> J[SKIP - log warning]
        I -- Yes --> K[Calculate Gross\nInsurance, Tax, Net]
        K --> L{Already Paid?}
        L -- Yes --> M[SKIP - never recalculate Paid]
        L -- No --> N[Upsert PayrollEntity]
        N --> H
    end

    subgraph "Bước 3: Phê duyệt & Thanh toán"
        O[POST /api/payrolls/id/status\nbody: Approved] --> P
        P[Status: Draft → Approved] --> Q
        Q[POST /api/payrolls/id/status\nbody: Paid, paidDate] --> R
        R[Status: Approved → Paid]
    end

    C --> D
    N --> O
    R --> S[GET /api/payrolls/id/pdf\nDownload payslip PDF\nvia QuestPDF]
    R --> T[GET /api/payrolls/export\nExport Excel\nvia ClosedXML]

    style C fill:#2980b9,color:#fff
    style N fill:#27ae60,color:#fff
    style R fill:#8e44ad,color:#fff
```

---

### 5.2 Công thức tính lương

```mermaid
flowchart TD
    A["📥 Đầu vào từ PayrollCycle"]
    A --> B["standardWorkingDays = N\n(snapshot cố định, không đổi)"]

    C["📥 Từ Contract"]
    C --> D["baseSalary\ntransportAllowance\nlunchAllowance\notherAllowance"]

    E["📥 Từ AttendanceBucket"]
    E --> F["actualPayableDays\ntotalOvertimeHours"]

    B & D & F --> G["💰 Tính Gross Income"]
    G --> H["hourlyRate = baseSalary / stdDays / 8\novertimePay = hours × hourlyRate × overtimeRate"]
    H --> I["grossIncome = (baseSalary + allowances)\n/ stdDays × actualDays + overtimePay"]

    I --> J["📋 Tính Bảo hiểm"]
    J --> K["insuranceSalary = min(baseSalary, insuranceSalaryCap)\nBHXH = insuranceSalary × socialRate\nBHYT = insuranceSalary × healthRate\nBHTN = insuranceSalary × unemploymentRate"]

    I & K --> L["📋 Tính Thuế TNCN (PIT)"]
    L --> M["incomeBeforeTax = gross - (BHXH + BHYT + BHTN)\npersonalDeduction = 11M + dependents × 4.4M\ntaxableIncome = max(0, incomeBeforeTax - personalDeduction)"]
    M --> N["VietnameseTaxCalculator\n(7 bậc lũy tiến: 5%→35%)"]

    L & N --> O["💵 Tính Net Salary"]
    O --> P["prevDebt = previous month debtAmount (if any)\nnetSalary = gross - BHXH - BHYT - BHTN - tax - prevDebt\nnewDebt = netSalary < 0 ? abs(netSalary) : 0\nif netSalary < 0: netSalary = 0"]

    style I fill:#f39c12,color:#fff
    style N fill:#e74c3c,color:#fff
    style P fill:#27ae60,color:#fff
```

---

### 5.3 Vòng đời trạng thái bảng lương

```mermaid
stateDiagram-v2
    [*] --> Draft : GeneratePayroll\ncalculated
    Draft --> Calculated : (deprecated — mapped to Draft)
    Draft --> Approved : Admin/HR approves\nPOST /payrolls/{id}/status { Approved }
    Draft --> Draft : Re-generate\n(recalculate if not Paid)
    Approved --> Paid : Admin/HR marks paid\n{ Paid, paidDate }
    Paid --> [*]

    note right of Paid
        Trạng thái CUỐI — không thể thay đổi.
        Tất cả các lần generate lại sẽ SKIP
        employee này (log warning).
    end note
```

---

## 6. Recruitment Pipeline

### 6.1 Từ Vacancy đến Onboarding

```mermaid
flowchart TD
    A[POST /recruitment/vacancies\nTạo tin tuyển dụng] --> B
    B{Vacancy Status}
    B -- Open --> C[POST /recruitment/candidates\nỨng viên apply]
    B -- Closed --> Z[Không nhận hồ sơ mới]

    C --> D[POST /candidates/id/status\nApplied → Screening]
    D --> E[POST /recruitment/interviews\nLên lịch phỏng vấn]
    E --> F[POST /interviews/id/review\nGhi kết quả, outcome]

    F --> G{Kết quả?}
    G -- Pass --> H[POST /candidates/id/status\nScreening → Interview → Offered]
    G -- Fail --> I[POST /candidates/id/status\nStatus = Rejected]

    H --> J[POST /candidates/id/status\nOffered → Hired]
    J --> K[POST /candidates/id/onboard\nHired → Onboard]
    K --> L[EmployeeEntity created\nStatus = Probation]
    L --> M[POST /recruitment/vacancies/id/close\nĐóng tin nếu đủ người]

    style K fill:#27ae60,color:#fff
    style I fill:#e74c3c,color:#fff
    style L fill:#2980b9,color:#fff
```

---

### 6.2 Onboard Candidate → Employee

```mermaid
sequenceDiagram
    participant HR as HR Admin
    participant API as POST /recruitment/candidates/{id}/onboard
    participant Handler as OnboardCandidateHandler
    participant CandRepo as ICandidateRepository
    participant EmpRepo as IEmployeeRepository
    participant UoW as IUnitOfWork
    participant Pub as IPublisher

    HR->>API: POST /candidates/{id}/onboard\n{ employeeCode, joinDate, deptId, positionId, managerId, dob }
    API->>Handler: Send(OnboardCandidateCommand)

    Handler->>CandRepo: GetByIdAsync(candidateId)
    alt Not found
        Handler-->>API: throw ValidationException
    end

    alt candidate.Status != Hired
        Handler-->>API: throw ValidationException("Only Hired can be onboarded")
    end

    Handler->>EmpRepo: ExistsByCodeAsync(employeeCode)
    alt Code exists
        Handler-->>API: throw ValidationException("Code already exists")
    end

    Handler->>UoW: BeginTransactionAsync()

    Handler->>Handler: new EmployeeEntity(\n  code, candidate.FullName, candidate.Email\n)
    Handler->>Handler: employee.UpdateJobDetails(\n  deptId, positionId, status = Probation\n)
    Handler->>Handler: employee.UpdatePersonalInfo(\n  phone=candidate.Phone, dob\n)

    Handler->>EmpRepo: CreateAsync(employee)
    Handler->>Handler: candidate.UpdateStatus(Onboarded)
    Handler->>CandRepo: UpdateAsync(candidate)

    Handler->>Pub: Publish(EmployeeCreatedEvent)
    Note over Pub: CreateUserEventHandler\n→ Hangfire EnqueueAccountProvisioning

    Handler->>UoW: CommitTransactionAsync()
    Note over UoW: Nếu bất kỳ bước nào fail → RollbackTransactionAsync()

    Handler-->>API: employee.Id
    API-->>HR: 201 Created { employeeId }
```

---

### 6.3 State Machine — Candidate

```mermaid
stateDiagram-v2
    [*] --> Applied : POST /candidates\n(nộp hồ sơ)

    Applied --> Screening : HR review hồ sơ
    Applied --> Rejected : Loại từ vòng CV

    Screening --> Interview : Passed screening
    Screening --> Rejected : Failed screening

    Interview --> Offered : Phỏng vấn pass
    Interview --> Rejected : Phỏng vấn fail

    Offered --> Hired : Ứng viên chấp nhận offer
    Offered --> Rejected : Ứng viên từ chối

    Hired --> Onboarded : Onboard thành công\n→ EmployeeEntity tạo

    Rejected --> [*]
    Onboarded --> [*]

    note right of Onboarded
        candidate.Status = Onboarded
        EmployeeEntity.Status = Probation
        Account provisioned via Hangfire
    end note
```

---

## 7. Background Services Schedule

**Tất cả 5 background services** chạy liên tục trong vòng đời ứng dụng (`BackgroundService`), mỗi service có interval cấu hình được qua `appsettings.json`.

```mermaid
gantt
    title Background Jobs Timeline (illustrative)
    dateFormat HH:mm
    axisFormat %H:%M

    section Attendance Processing
    Sweep (every 5 min)     :a1, 00:00, 5m
    Sweep                   :a2, 00:05, 5m
    Sweep                   :a3, 00:10, 5m

    section Leave Accrual
    Check (every 6h)        :l1, 00:00, 6h
    Check                   :l2, 06:00, 6h

    section Payroll Auto-Calculate
    Check (every 12h)       :p1, 00:00, 12h
    Check                   :p2, 12:00, 12h

    section Contract Expiry
    Check (periodic)        :c1, 00:00, 8h

    section Soft Delete Cleanup
    Cleanup (periodic)      :s1, 02:00, 2h
```

**Trigger conditions:**

| Service | Interval | Điều kiện chạy thực tế |
|---|---|---|
| `AttendanceProcessingBackgroundJob` | 5 phút | Luôn chạy — xử lý raw logs tồn đọng |
| `LeaveAccrualBackgroundService` | 6 giờ | Chỉ accrual khi đang ở ngày 1 của tháng |
| `PayrollBackgroundService` | 12 giờ | Chỉ chạy khi `day >= 28` hoặc `day == 1` |
| `ContractExpirationBackgroundService` | Cấu hình | Gửi notification khi hợp đồng sắp hết hạn |
| `SoftDeleteCleanupBackgroundService` | Cấu hình | Xóa vật lý tài liệu đã soft-delete quá lâu |

**Retry logic (tất cả service):**

```mermaid
flowchart LR
    A[Sweep triggered] --> B{Execute}
    B -- Success --> C[Log info, sleep]
    B -- Fail attempt 1 --> D[Wait 10s, retry]
    D --> B
    B -- Fail attempt 2 --> E[Wait 20s, retry]
    E --> B
    B -- Fail attempt 3 --> F[Log error, sleep until next interval]
```

---

## 8. Domain Event Bus

Hệ thống dùng **MediatR `IPublisher`** cho in-process domain events theo pattern `DomainEventNotification<TEvent>`.

```mermaid
flowchart LR
    subgraph "Domain Events Published"
        E1[EmployeeCreatedEvent]
        E2[EmployeeDeletedEvent]
        E3[EmployeeUpdatedEvent]
        E4[LeaveRequestSubmittedEvent]
        E5[LeaveRequestApprovedEvent]
        E6[LeaveRequestRejectedEvent]
    end

    subgraph "Event Handlers (MediatR INotificationHandler)"
        H1[CreateUserEventHandler\nEnqueue Hangfire job\n→ Account provisioning]
        H2[EmployeeDeletedEventHandler\nRevoke Identity user]
        H3[EmployeeUpdatedEventHandler\nSync Identity user data]
        H4[LeaveRequestSubmittedEventHandler\nAuditLog: SUBMIT_LEAVE_REQUEST]
        H5[LeaveRequestApprovedEventHandler\nNotification: 'Leave Approved ✅']
        H6[LeaveRequestRejectedEventHandler\nNotification: 'Leave Rejected ❌']
    end

    E1 -->|MediatR Publish| H1
    E2 -->|MediatR Publish| H2
    E3 -->|MediatR Publish| H3
    E4 -->|MediatR Publish| H4
    E5 -->|MediatR Publish| H5
    E6 -->|MediatR Publish| H6

    H1 -->|Hangfire| HANGFIRE[(Hangfire\nMongoDB Queue)]
    H5 --> NOTIF[(notifications\nMongoDB)]
    H6 --> NOTIF
    H4 --> AUDIT[(audit_logs\nMongoDB)]
    H2 --> IDENTITY[(ASP.NET Identity\nMongoDB)]

    style HANGFIRE fill:#e67e22,color:#fff
    style NOTIF fill:#2980b9,color:#fff
    style AUDIT fill:#7f8c8d,color:#fff
    style IDENTITY fill:#8e44ad,color:#fff
```

---

## 9. Request Pipeline (Middleware / CQRS)

Mỗi HTTP request đi qua các lớp middleware trước khi đến handler, và response đi ngược lại.

```mermaid
flowchart TD
    REQ[HTTP Request] --> RateLimit[Rate Limiting\nPartitionedRateLimiter]
    RateLimit --> CORS[CORS Policy]
    CORS --> SWAGGER[Swagger / OpenAPI\ndev only]
    SWAGGER --> AUTH_MW[Authentication Middleware\nJWT Bearer validation]
    AUTH_MW --> AUTHZ_MW[Authorization Middleware\nRole checks]
    AUTHZ_MW --> HANGFIRE_FILTER[HangfireAuthFilter\nAdmin-only access\nto /hangfire]
    HANGFIRE_FILTER --> EXCEPTION[GlobalExceptionHandlerMiddleware\nApiResponse wrapper]
    EXCEPTION --> CARTER[Carter Route Matching\nICarterModule endpoints]
    CARTER --> VALIDATOR[FluentValidation\nCommand validators]
    VALIDATOR --> MEDIATR[MediatR Pipeline\nIPipelineBehavior]

    subgraph "MediatR Behaviors"
        MEDIATR --> LOG_BEH[LoggingBehavior\nlog command + duration]
        LOG_BEH --> VALID_BEH[ValidationBehavior\nautomatically runs validators]
        VALID_BEH --> HANDLER[Command/Query Handler]
    end

    HANDLER --> REPO[Repository\nMongoDB Driver]
    REPO --> MONGODB[(MongoDB Atlas)]
    MONGODB --> REPO
    REPO --> HANDLER
    HANDLER --> MEDIATR
    MEDIATR --> CARTER
    CARTER --> EXCEPTION
    EXCEPTION --> RESP[HTTP Response\nApiResponse<T> JSON]

    style REQ fill:#2c3e50,color:#fff
    style RESP fill:#27ae60,color:#fff
    style MONGODB fill:#4db33d,color:#fff
    style EXCEPTION fill:#e74c3c,color:#fff
```

**Exception → HTTP Status mapping** (thực hiện trong `GlobalExceptionHandlerMiddleware`):

| Exception | HTTP Status | ErrorCode |
|---|---|---|
| `ValidationException` | 400 | `VALIDATION_ERROR` |
| `NotFoundException` | 404 | `NOT_FOUND` |
| `ConflictException` | 409 | `CONFLICT` |
| `ConcurrencyException` | 409 | `CONCURRENCY_CONFLICT` |
| `UnauthorizedAccessException` | 401 | `UNAUTHORIZED` |
| `ForbiddenException` | 403 | `FORBIDDEN` |
| `InvalidOperationException` | 409 | `INVALID_STATE` |
| Unhandled `Exception` | 500 | `INTERNAL_ERROR` + correlationId |
