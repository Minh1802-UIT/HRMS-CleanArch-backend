# API Endpoint Reference — HRMS

> **Document Version:** 1.1  
> **Last Updated:** June 2025  
> **Author:** Senior Developer  
> **Base URL (Dev):** `http://localhost:5000`  
> **Base URL (Prod):** `https://hrms-api.onrender.com`  
> **API Version:** `v1` (header `X-Api-Version: 1` hoặc prefix URL)

---

## Table of Contents

1. [Conventions & Standards](#1-conventions--standards)
2. [Authentication](#2-authentication)
3. [Human Resource — Employees](#3-human-resource--employees)
4. [Human Resource — Contracts](#4-human-resource--contracts)
5. [Organization — Departments](#5-organization--departments)
6. [Organization — Positions](#6-organization--positions)
7. [Recruitment — Job Vacancies](#7-recruitment--job-vacancies)
8. [Recruitment — Candidates](#8-recruitment--candidates)
9. [Recruitment — Interviews](#9-recruitment--interviews)
10. [Attendance — Operations](#10-attendance--operations)
11. [Attendance — Shifts](#11-attendance--shifts)
12. [Leave Management — Types](#12-leave-management--types)
13. [Leave Management — Requests](#13-leave-management--requests)
14. [Leave Management — Allocations](#14-leave-management--allocations)
15. [Payroll — Cycles](#15-payroll--cycles)
16. [Payroll — Public Holidays](#16-payroll--public-holidays)
17. [Payroll — Payrolls](#17-payroll--payrolls)
18. [Performance Management](#18-performance-management)
19. [Notifications](#19-notifications)
20. [Dashboard](#20-dashboard)
21. [Audit Logs](#21-audit-logs)
22. [File Upload](#22-file-upload)
23. [Dev Tools](#23-dev-tools)
24. [Rate Limiting](#24-rate-limiting)
25. [Error Codes Reference](#25-error-codes-reference)

---

## 1. Conventions & Standards

### 1.1 Response Envelope

Mọi response đều bọc trong `ApiResponse<T>`:

```json
{
  "succeeded": true,
  "message": "Retrieved successfully.",
  "data": { ... },
  "errors": null,
  "errorCode": null
}
```

**Khi lỗi:**

```json
{
  "succeeded": false,
  "message": "Employee not found.",
  "data": null,
  "errors": ["Employee with id 'abc123' does not exist."],
  "errorCode": "NOT_FOUND"
}
```

### 1.2 HTTP Status Codes

| Code | Ý nghĩa |
|---|---|
| `200 OK` | Thành công (GET, PATCH, POST action không tạo resource mới) |
| `201 Created` | Tạo resource thành công (POST) |
| `400 Bad Request` | Validation lỗi — field errors trong `errors[]` |
| `401 Unauthorized` | Chưa đăng nhập hoặc token hết hạn |
| `403 Forbidden` | Không đủ quyền (role không phù hợp) |
| `404 Not Found` | Resource không tồn tại |
| `409 Conflict` | Xung đột dữ liệu (duplicate, state không hợp lệ) |
| `429 Too Many Requests` | Vượt rate limit — có header `Retry-After` |
| `500 Internal Server Error` | Lỗi server — có `correlationId` để trace |

### 1.3 Pagination (offset-based)

Các endpoint danh sách hỗ trợ `PaginationParams` qua query string:

| Query Param | Type | Default | Mô tả |
|---|---|---|---|
| `pageNumber` | `int` | `1` | Trang hiện tại |
| `pageSize` | `int` | `10` | Số item mỗi trang |
| `searchTerm` | `string` | `null` | Tìm kiếm toàn văn |

**Response data dạng paged:**

```json
{
  "items": [...],
  "totalCount": 120,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 12
}
```

### 1.4 Authorization Header

```
Authorization: Bearer <accessToken>
```

> **Refresh Token** được lưu trong **httpOnly cookie** (`refreshToken`), không có trong header hay body.

### 1.5 Method Convention

| Method | Semantic |
|---|---|
| `GET` | Đọc data, không thay đổi state |
| `POST` | Tạo resource mới hoặc trigger action |
| `PATCH` | Cập nhật một phần resource |
| `PUT` | Thay thế hoàn toàn resource |
| `DELETE` | Xóa mềm (soft delete) |

---

## 2. Authentication

**Base path:** `/api/auth`  
**Rate limit policy:** `auth` (10 req/min/IP cho endpoints đăng nhập)

---

### `POST /api/auth/login`

Đăng nhập, lấy access token. Refresh token được lưu vào **httpOnly cookie** `SameSite=None; Secure`.

**Auth:** Không yêu cầu  
**Rate limit:** `auth`

**Request body:**

```json
{
  "username": "admin@company.com",
  "password": "Admin@123456"
}
```

**Response `200`:**

```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": "a1b2c3d4-...",
      "username": "admin@company.com",
      "email": "admin@company.com",
      "fullName": "Nguyen Van A",
      "employeeId": "66f1234abc...",
      "roles": ["Admin"],
      "isActive": true,
      "mustChangePassword": false
    }
  }
}
```

> **Cookie response header:**  
> `Set-Cookie: refreshToken=<token>; HttpOnly; Secure; SameSite=None; Path=/; Expires=...`

---

### `POST /api/auth/refresh-token`

Làm mới access token bằng refresh token trong cookie.

**Auth:** Không yêu cầu  
**Rate limit:** `refresh`

**Request body:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

> Refresh token đọc tự động từ httpOnly cookie `refreshToken`.

**Response `200`:**

```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 3600
  }
}
```

---

### `POST /api/auth/logout`

Đăng xuất — thu hồi tất cả refresh token trên server, xóa cookie.

**Auth:** Không yêu cầu (jwt optional — thu hồi nếu có userId trong claims)  
**Rate limit:** `refresh`

**Request body:** _(trống)_

**Response `200`:** `"Logged out successfully."`

---

### `POST /api/auth/register`

Tạo tài khoản người dùng mới.

**Auth:** `Admin`

**Request body:**

```json
{
  "username": "john.doe",
  "email": "john.doe@company.com",
  "password": "SecurePass@123",
  "fullName": "John Doe",
  "employeeId": "66f1234abc...",
  "mustChangePassword": true
}
```

**Response `201`:** `"Account registered successfully."`

---

### `POST /api/auth/forgot-password`

Gửi email đặt lại mật khẩu.

**Auth:** Không yêu cầu

**Request body:**

```json
{ "email": "user@company.com" }
```

**Response `200`:** `"If the email exists, a reset link has been sent."`  
> Token reset **không** trả về trong response — chỉ gửi qua email.

---

### `POST /api/auth/reset-password`

Đặt lại mật khẩu bằng token từ email.

**Auth:** Không yêu cầu

**Request body:**

```json
{
  "email": "user@company.com",
  "token": "<reset-token-from-email>",
  "newPassword": "NewPass@123",
  "confirmPassword": "NewPass@123"
}
```

**Response `200`:** `"Password has been reset successfully."`

---

### `POST /api/auth/change-password`

Đổi mật khẩu (người dùng đã đăng nhập).

**Auth:** Bất kỳ user đã đăng nhập

**Request body:**

```json
{
  "currentPassword": "OldPass@123",
  "newPassword": "NewPass@456",
  "confirmPassword": "NewPass@456"
}
```

**Response `200`:** `"Password changed successfully."`

---

### `GET /api/auth/users`

Lấy danh sách tất cả user.

**Auth:** `Admin`, `HR`  
**Query params:** `pageNumber`, `pageSize`, `searchTerm`

**Response `200`:** Danh sách `UserDto` (paged).

---

### `PATCH /api/auth/roles/{userId}`

Cập nhật danh sách roles của một user.

**Auth:** `Admin`

**Request body:**

```json
{ "roles": ["HR", "Manager"] }
```

**Response `200`:** `"User roles updated successfully."`

---

### `POST /api/auth/status/{userId}`

Kích hoạt / vô hiệu hóa tài khoản.

**Auth:** `Admin`, `HR`

**Request body:**

```json
{ "isActive": false }
```

**Response `200`:** `"User account has been deactivated."`

---

### `GET /api/auth/roles`

Lấy danh sách tất cả roles.

**Auth:** `Admin`

**Response `200`:**

```json
{ "data": ["Admin", "HR", "Manager", "Employee"] }
```

---

### `POST /api/auth/role`

Tạo role mới.

**Auth:** `Admin`

**Request body:**

```json
{ "roleName": "Accountant" }
```

**Response `201`:** `"Role 'Accountant' created successfully."`

---

### `POST /api/auth/assign-role`

Gán role cho user.

**Auth:** `Admin`

**Request body:**

```json
{
  "username": "john.doe",
  "roleName": "HR"
}
```

**Response `200`:** `"Role 'HR' assigned to user 'john.doe' successfully."`

---

## 3. Human Resource — Employees

**Base path:** `/api/employees`  
**Auth mặc định:** Đăng nhập là bắt buộc

---

### `GET /api/employees`

Lấy danh sách nhân viên (phân trang).

**Auth:** Bất kỳ user đã đăng nhập  
**Query params:** `pageNumber`, `pageSize`, `searchTerm`

**Response `200`:** Paged list of `EmployeeListSummary`:

```json
{
  "data": {
    "items": [
      {
        "id": "66f1234abc...",
        "employeeCode": "NV001",
        "fullName": "Nguyen Van A",
        "email": "nva@company.com",
        "avatarUrl": "https://...",
        "departmentName": "Engineering",
        "positionName": "Senior Developer",
        "jobStatus": "Active",
        "joinDate": "2024-01-15T00:00:00Z"
      }
    ],
    "totalCount": 85,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 9
  }
}
```

---

### `GET /api/employees/lookup`

Lấy danh sách nhân viên dạng dropdown (id + tên).

**Auth:** Bất kỳ user đã đăng nhập  
**Query params:**

| Param | Type | Mô tả |
|---|---|---|
| `keyword` | `string?` | Tìm kiếm theo tên hoặc mã |
| `limit` | `int?` | Số tối đa trả về (default `20`) |

**Response `200`:**

```json
{
  "data": [
    { "id": "66f1234abc...", "label": "NV001 - Nguyen Van A" }
  ]
}
```

---

### `GET /api/employees/org-chart`

Lấy cây sơ đồ tổ chức dựa trên `managerId`.

**Auth:** Bất kỳ user đã đăng nhập

**Response `200`:** Cây `EmployeeOrgNodeDto` đệ quy:

```json
{
  "data": [
    {
      "id": "...", "name": "CEO Name", "title": "CEO", "avatarUrl": "...",
      "departmentId": "...",
      "children": [
        { "id": "...", "name": "CTO Name", "title": "CTO", "children": [...] }
      ]
    }
  ]
}
```

---

### `GET /api/employees/{id}`

Lấy chi tiết một nhân viên.

**Auth:** Bất kỳ user đã đăng nhập

**Response `200`:** `EmployeeDto` đầy đủ (bao gồm `personalInfo`, `jobDetails`, `bankDetails`).

---

### `POST /api/employees`

Tạo nhân viên mới.

**Auth:** `Admin`, `HR`

**Request body:**

```json
{
  "employeeCode": "NV099",
  "fullName": "Tran Thi B",
  "email": "ttb@company.com",
  "avatarUrl": "https://...",
  "personalInfo": {
    "dateOfBirth": "1995-06-15T00:00:00Z",
    "gender": "Female",
    "phoneNumber": "0901234567",
    "identityCard": "012345678910",
    "address": "123 Le Loi, Q1, HCM",
    "maritalStatus": "Single",
    "nationality": "Vietnamese",
    "hometown": "Ha Noi",
    "country": "Vietnam",
    "city": "Ho Chi Minh",
    "postalCode": "700000"
  },
  "jobDetails": {
    "departmentId": "66d0001abc...",
    "positionId": "66d0002def...",
    "managerId": "66d0003xyz...",
    "shiftId": "66d0004uvw...",
    "joinDate": "2026-03-01T00:00:00Z",
    "status": "Active",
    "probationEndDate": "2026-06-01T00:00:00Z"
  },
  "bankDetails": {
    "bankName": "Vietcombank",
    "accountNumber": "1234567890",
    "accountHolder": "TRAN THI B",
    "insuranceCode": "GD1234567",
    "taxCode": "1234567890"
  }
}
```

**Response `201`:** `EmployeeDto` vừa tạo.

---

### `PATCH /api/employees/{id}`

Cập nhật thông tin nhân viên.

**Auth:** `Admin`, `HR`

**Request body:** Tương tự `CreateEmployeeDto` +  `id` và `version` (optimistic concurrency):

```json
{
  "id": "66f1234abc...",
  "version": 3,
  "fullName": "Tran Thi B (Updated)",
  ...
}
```

> `version` phải khớp với bản ghi hiện tại — nếu không trả `409 Conflict`.

**Response `200`:** `"Employee updated successfully via CQRS."`

---

### `DELETE /api/employees/{id}`

Xóa mềm nhân viên.

**Auth:** `Admin`

**Response `200`:** `"Employee deleted successfully via CQRS."`

---

## 4. Human Resource — Contracts

**Base path:** `/api/contracts`  
**Auth mặc định:** Đăng nhập là bắt buộc

---

### `GET /api/contracts`

Danh sách hợp đồng (phân trang, tất cả nhân viên).

**Auth:** `Admin`, `HR`  
**Query params:** `pageNumber`, `pageSize`, `searchTerm`

---

### `GET /api/contracts/{id}`

Chi tiết một hợp đồng.

**Auth:** `Admin`, `HR`

---

### `GET /api/contracts/employee/{employeeId}`

Danh sách hợp đồng của một nhân viên cụ thể.

**Auth:** `Admin`, `HR`

---

### `GET /api/contracts/me`

Danh sách hợp đồng của chính người dùng hiện tại.

**Auth:** Bất kỳ user đã đăng nhập (self-service)

---

### `POST /api/contracts`

Tạo hợp đồng mới.

**Auth:** `Admin`, `HR`

**Request body:**

```json
{
  "employeeId": "66f1234abc...",
  "contractCode": "HD-2026-001",
  "type": "Fixed-Term",
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": "2027-01-01T00:00:00Z",
  "status": "Active",
  "salary": {
    "basicSalary": 15000000,
    "transportAllowance": 500000,
    "lunchAllowance": 730000,
    "otherAllowance": 0
  },
  "fileUrl": "https://storage.example.com/contracts/HD-2026-001.pdf"
}
```

**Response `201`:** Contract vừa tạo.

---

### `PATCH /api/contracts/{id}`

Cập nhật hợp đồng.

**Auth:** `Admin`, `HR`

---

### `DELETE /api/contracts/{id}`

Xóa mềm hợp đồng.

**Auth:** `Admin`

---

## 5. Organization — Departments

**Base path:** `/api/departments`  
**Auth mặc định:** Đăng nhập là bắt buộc

---

### `GET /api/departments`

Danh sách phòng ban (phân trang).

**Auth:** Bất kỳ user đã đăng nhập  
**Query params:** `pageNumber`, `pageSize`, `searchTerm`

---

### `GET /api/departments/tree`

Lấy cây phòng ban (quan hệ cha-con `parentId`).

**Auth:** Bất kỳ user đã đăng nhập

**Response `200`:** Mảng cây đệ quy.

---

### `GET /api/departments/{id}`

Chi tiết một phòng ban.

**Auth:** Bất kỳ user đã đăng nhập

---

### `POST /api/departments`

Tạo phòng ban mới.

**Auth:** `Admin`

**Request body:**

```json
{
  "name": "Engineering",
  "code": "ENG",
  "description": "Software engineering department",
  "managerId": "66f1234abc...",
  "parentId": null
}
```

---

### `PATCH /api/departments/{id}`

Cập nhật phòng ban.

**Auth:** `Admin`

---

### `DELETE /api/departments/{id}`

Xóa mềm phòng ban.

**Auth:** `Admin`

---

## 6. Organization — Positions

**Base path:** `/api/positions`  
**Auth mặc định:** Đăng nhập là bắt buộc

---

| Method | Path | Auth | Mô tả |
|---|---|---|---|
| `GET` | `/api/positions` | Any | Danh sách chức vụ (paged) |
| `GET` | `/api/positions/tree` | Any | Cây phân cấp chức vụ |
| `GET` | `/api/positions/{id}` | Any | Chi tiết chức vụ |
| `POST` | `/api/positions` | `Admin`, `HR` | Tạo chức vụ mới |
| `PATCH` | `/api/positions/{id}` | `Admin`, `HR` | Cập nhật chức vụ |
| `DELETE` | `/api/positions/{id}` | `Admin` | Xóa mềm chức vụ |

**Request body tạo/cập nhật:**

```json
{
  "title": "Senior Backend Developer",
  "code": "SBE",
  "departmentId": "66d0001abc...",
  "parentId": "66d9999xyz...",
  "salaryRange": {
    "min": 20000000,
    "max": 40000000,
    "currency": "VND"
  }
}
```

---

## 7. Recruitment — Job Vacancies

**Base path:** `/api/recruitment/vacancies`  
**Auth:** `Admin`, `HR` (toàn bộ module)

---

| Method | Path | Mô tả |
|---|---|---|
| `GET` | `/api/recruitment/vacancies` | Danh sách tin tuyển dụng |
| `GET` | `/api/recruitment/vacancies/{id}` | Chi tiết tin tuyển dụng |
| `POST` | `/api/recruitment/vacancies` | Tạo tin tuyển dụng |
| `PATCH` | `/api/recruitment/vacancies/{id}` | Cập nhật tin tuyển dụng |
| `POST` | `/api/recruitment/vacancies/{id}/close` | Đóng tin tuyển dụng |
| `DELETE` | `/api/recruitment/vacancies/{id}` | Xóa mềm tin tuyển dụng |

**Request body tạo:**

```json
{
  "title": "Backend Developer .NET",
  "description": "Looking for experienced .NET developer...",
  "vacancies": 3,
  "expiredDate": "2026-04-30T00:00:00Z",
  "status": "Open",
  "requirements": ["C#", ".NET 8", "MongoDB", "Clean Architecture"]
}
```

---

## 8. Recruitment — Candidates

**Base path:** `/api/recruitment/candidates`  
**Auth:** `Admin`, `HR` (toàn bộ module)

---

| Method | Path | Mô tả |
|---|---|---|
| `GET` | `/api/recruitment/candidates?vacancyId={id}` | Danh sách ứng viên theo tin tuyển dụng |
| `GET` | `/api/recruitment/candidates/{id}` | Chi tiết ứng viên |
| `POST` | `/api/recruitment/candidates` | Tạo ứng viên mới |
| `PATCH` | `/api/recruitment/candidates/{id}` | Cập nhật ứng viên |
| `POST` | `/api/recruitment/candidates/{id}/status` | Cập nhật trạng thái ứng viên |
| `POST` | `/api/recruitment/candidates/{id}/onboard` | Chuyển ứng viên thành nhân viên |
| `DELETE` | `/api/recruitment/candidates/{id}` | Xóa mềm ứng viên |

**Request body `POST /candidates/{id}/status`:**

```json
{ "status": "Interview" }
```

> `CandidateStatus`: `Applied` → `Screening` → `Interview` → `Offered` → `Hired` | `Rejected`

**Request body `POST /candidates/{id}/onboard`:**

```json
{
  "employeeCode": "NV100",
  "joinDate": "2026-04-01T00:00:00Z",
  "departmentId": "66d0001abc...",
  "positionId": "66d0002def...",
  "shiftId": "66d0003uvw..."
}
```

---

## 9. Recruitment — Interviews

**Base path:** `/api/recruitment/interviews`  
**Auth:** `Admin`, `HR` (toàn bộ module)

---

| Method | Path | Mô tả |
|---|---|---|
| `GET` | `/api/recruitment/interviews?candidateId={id}` | Danh sách phỏng vấn theo ứng viên |
| `GET` | `/api/recruitment/interviews/{id}` | Chi tiết lịch phỏng vấn |
| `POST` | `/api/recruitment/interviews` | Tạo lịch phỏng vấn |
| `PATCH` | `/api/recruitment/interviews/{id}` | Cập nhật lịch phỏng vấn |
| `POST` | `/api/recruitment/interviews/{id}/review` | Ghi kết quả phỏng vấn |
| `DELETE` | `/api/recruitment/interviews/{id}` | Xóa lịch phỏng vấn |

**Request body tạo lịch phỏng vấn:**

```json
{
  "candidateId": "66c001abc...",
  "interviewerId": "66f1234abc...",
  "scheduledTime": "2026-03-20T09:00:00Z",
  "durationMinutes": 60,
  "location": "Online - Google Meet",
  "status": "Scheduled"
}
```

**Request body `POST /interviews/{id}/review`:**

```json
{
  "status": "Completed",
  "feedback": "Candidate showed strong technical skills in .NET and MongoDB..."
}
```

---

## 10. Attendance — Operations

**Base path:** `/api/attendance`  
**Auth mặc định:** Đăng nhập là bắt buộc

---

### `POST /api/attendance/check-in`

Chấm công vào.

**Auth:** Bất kỳ user đã đăng nhập  
**Rate limit:** `checkin` (10 lần/giờ/user)

**Request body:**

```json
{
  "employeeId": "66f1234abc...",
  "timestamp": "2026-03-06T08:02:00Z",
  "type": "CheckIn",
  "deviceId": "DEVICE-001",
  "latitude": 10.7769,
  "longitude": 106.7009
}
```

**Response `200`:** `"Check-in recorded successfully."`

> Raw log được lưu ngay (nhanh). Background job xử lý sau mỗi 5 phút.

---

### `POST /api/attendance/check-out`

Chấm công ra. Request body giống `check-in` với `type: "CheckOut"`.

**Rate limit:** `checkin` (dùng chung bucket với check-in)

---

### `GET /api/attendance/me/today-status`

Xem trạng thái chấm công hôm nay của bản thân (check-in, check-out, overtime...).

**Auth:** Bất kỳ user đã đăng nhập

---

### `GET /api/attendance/me/range`

Xem chấm công của bản thân trong khoảng thời gian.

**Auth:** Bất kỳ user đã đăng nhập  
**Query params:** `startDate`, `endDate` (ISO 8601)

---

### `GET /api/attendance/me/report`

Báo cáo chấm công tháng của bản thân.

**Auth:** Bất kỳ user đã đăng nhập  
**Query params:** `month` (format `MM-YYYY`, e.g., `03-2026`)

---

### `GET /api/attendance/team/summary`

Tổng hợp chấm công của toàn team.

**Auth:** `Admin`, `HR`, `Manager`  
**Query params:** `month` (MM-YYYY)

---

### `GET /api/attendance/employee/{employeeId}/report`

Báo cáo chấm công của một nhân viên cụ thể.

**Auth:** `Admin`, `HR`, `Manager`  
**Query params:** `month` (MM-YYYY)

---

### `GET /api/attendance/daily/{dateStr}`

Báo cáo ngày (dashboard) — toàn công ty ngày đó.

**Auth:** `Admin`, `HR`, `Manager`  
**Path param:** `dateStr` — format `yyyy-MM-dd` (e.g., `2026-03-06`)

---

### `POST /api/attendance/process-logs`

Kích hoạt thủ công xử lý raw logs → attendance buckets.

**Auth:** `Admin`, `HR`

**Request body:** _(trống)_

**Response `200`:** Số lượng log đã xử lý.

---

### `POST /api/attendance/admin/force-reprocess`

Xử lý lại toàn bộ attendance của một tháng (sửa dữ liệu bị hỏng).

**Auth:** `Admin`  
**Query params:** `month` (MM-YYYY)

---

### `POST /api/attendance/admin/backfill-holidays`

Cập nhật lại cờ ngày lễ cho dữ liệu attendance cũ.

**Auth:** `Admin`, `HR`

**Request body:**

```json
{ "month": 3, "year": 2026 }
```

---

### `POST /api/attendance/explanation`

Nhân viên gửi đơn giải trình khi quên check-out (`isMissingPunch = true`).

**Auth:** Bất kỳ user đã đăng nhập

**Request body:**

```json
{
  "workDate": "2026-03-12T00:00:00Z",
  "reason": "Quên check-out do đi họp client đột xuất"
}
```

**Response `201`:** ID của đơn giải trình vừa tạo.

---

### `GET /api/attendance/explanation/me`

Danh sách đơn giải trình của bản thân.

**Auth:** Bất kỳ user đã đăng nhập

**Response `200`:** Danh sách `AttendanceExplanationDto`.

---

### `GET /api/attendance/explanation/pending`

Danh sách đơn giải trình đang chờ duyệt (toàn công ty).

**Auth:** `Admin`, `HR`, `Manager`

**Response `200`:** Danh sách đơn có `status = Pending`.

---

### `PUT /api/attendance/explanation/{id}/review`

Duyệt hoặc từ chối đơn giải trình.

**Auth:** `Admin`, `HR`, `Manager`

**Request body:**

```json
{
  "status": "Approved",
  "reviewNote": "Đã xác nhận với manager trực tiếp"
}
```

> `status`: `Approved` | `Rejected`  
> - `Approved` → hệ thống tự ghi đủ 8h công cho ngày giải trình.  
> - `Rejected` → ngày đó giữ nguyên 0 h (mất 1 công).

---

### `POST /api/attendance/overtime-schedule`

Tạo một bản ghi OT đã phê duyệt cho một nhân viên cụ thể.

**Auth:** `Admin`, `HR`

**Request body:**

```json
{
  "employeeId": "66f1234abc...",
  "date": "2026-03-20T00:00:00Z",
  "note": "Sprint deadline"
}
```

**Response `201`:** ID của bản ghi vừa tạo.

---

### `POST /api/attendance/overtime-schedule/bulk`

Tạo hàng loạt bản ghi OT cho nhiều nhân viên (tất cả active hoặc theo danh sách).

**Auth:** `Admin`, `HR`

**Request body:**

```json
{
  "employeeIds": ["66f1234abc...", "66f5678def..."],
  "date": "2026-03-20T00:00:00Z",
  "note": "Month-end closing"
}
```

> Trường `employeeIds` trống = áp dụng cho tất cả nhân viên active.

---

### `DELETE /api/attendance/overtime-schedule/{id}`

Xóa bản ghi OT đã phê duyệt.

**Auth:** `Admin`, `HR`

**Path param:** `id` — ObjectId của bản ghi overtime schedule.

**Response `200`:** `"Overtime schedule deleted successfully."`

---

### `GET /api/attendance/overtime-schedule`

Danh sách bản ghi OT đã phê duyệt theo tháng.

**Auth:** `Admin`, `HR`, `Manager`  
**Query params:** `month` (MM-YYYY)

**Response `200`:** Danh sách `OvertimeScheduleDto`.

---

## 11. Attendance — Shifts

**Base path:** `/api/shifts`  
**Auth mặc định:** Đăng nhập là bắt buộc

---

| Method | Path | Auth | Mô tả |
|---|---|---|---|
| `GET` | `/api/shifts` | Any | Danh sách ca làm việc (paged) |
| `GET` | `/api/shifts/lookup` | Any | Ca làm việc dạng dropdown |
| `GET` | `/api/shifts/{id}` | Any | Chi tiết ca làm việc |
| `POST` | `/api/shifts` | `Admin`, `HR` | Tạo ca làm việc |
| `PATCH` | `/api/shifts/{id}` | `Admin`, `HR` | Cập nhật ca làm việc |
| `DELETE` | `/api/shifts/{id}` | `Admin`, `HR` | Xóa mềm ca làm việc |

**Request body tạo/cập nhật:**

```json
{
  "name": "Ca Sáng",
  "code": "CA_SANG",
  "startTime": "08:00:00",
  "endTime": "17:00:00",
  "breakStartTime": "12:00:00",
  "breakEndTime": "13:00:00",
  "gracePeriodMinutes": 15,
  "overtimeThresholdMinutes": 15,
  "isOvernight": false,
  "standardWorkingHours": 8.0,
  "isActive": true
}
```

---

## 12. Leave Management — Types

**Base path:** `/api/leave-types`  
**Auth mặc định:** Đăng nhập là bắt buộc

---

| Method | Path | Auth | Mô tả |
|---|---|---|---|
| `GET` | `/api/leave-types` | Any | Danh sách loại nghỉ phép (paged) |
| `GET` | `/api/leave-types/{id}` | Any | Chi tiết loại nghỉ phép |
| `POST` | `/api/leave-types` | `Admin` | Tạo loại nghỉ phép |
| `PATCH` | `/api/leave-types/{id}` | `Admin` | Cập nhật loại nghỉ phép |
| `DELETE` | `/api/leave-types/{id}` | `Admin` | Xóa mềm loại nghỉ phép |

**Request body tạo:**

```json
{
  "name": "Nghỉ phép năm",
  "code": "ANNUAL",
  "defaultDaysPerYear": 12,
  "isAccrual": true,
  "accrualRatePerMonth": 1.0,
  "allowCarryForward": true,
  "maxCarryForwardDays": 5,
  "isSandwichRuleApplied": false,
  "isActive": true
}
```

---

## 13. Leave Management — Requests

**Base path:** `/api/leaves`

---

### `GET /api/leaves`

Danh sách tất cả đơn xin nghỉ (toàn công ty).

**Auth:** `Admin`, `HR`, `Manager`  
**Query params:** `pageNumber`, `pageSize`, `searchTerm`

---

### `GET /api/leaves/me`

Danh sách đơn xin nghỉ của bản thân.

**Auth:** Bất kỳ user đã đăng nhập

---

### `GET /api/leaves/{id}`

Chi tiết một đơn xin nghỉ.

**Auth:** Bất kỳ user đã đăng nhập

---

### `GET /api/leaves/employee/{employeeId}`

Lịch sử đơn xin nghỉ của một nhân viên cụ thể.

**Auth:** `Admin`, `HR`, `Manager`

---

### `POST /api/leaves`

Tạo đơn xin nghỉ.

**Auth:** Bất kỳ user đã đăng nhập  
**Rate limit:** `write` (30 mutations/min/user)

**Request body:**

```json
{
  "leaveType": "Annual",
  "fromDate": "2026-03-10T00:00:00Z",
  "toDate": "2026-03-12T00:00:00Z",
  "reason": "Du lịch nghỉ mát gia đình"
}
```

**Response `201`:** LeaveRequest vừa tạo.

---

### `PATCH /api/leaves/{id}`

Sửa đơn xin nghỉ (chỉ khi còn ở trạng thái `Pending`).

**Auth:** Chủ đơn (owner, kiểm tra trong handler)  
**Rate limit:** `write`

---

### `POST /api/leaves/{id}/cancel`

Hủy đơn xin nghỉ.

**Auth:** Chủ đơn  
**Rate limit:** `write`

**Response `200`:** `"Leave request cancelled successfully."`

---

### `POST /api/leaves/{id}/review`

Duyệt hoặc từ chối đơn xin nghỉ.

**Auth:** `Admin`, `HR`, `Manager`

**Request body:**

```json
{
  "status": "Approved",
  "managerComment": "Đã xem xét, đồng ý cho nghỉ."
}
```

> `status`: `Approved` | `Rejected`

---

## 14. Leave Management — Allocations

**Base path:** `/api/leave-allocations`

---

### `GET /api/leave-allocations/me`

Số ngày phép còn lại của bản thân (theo từng loại).

**Auth:** Bất kỳ user đã đăng nhập

**Response `200`:**

```json
{
  "data": [
    {
      "leaveTypeName": "Nghỉ phép năm",
      "leaveTypeCode": "ANNUAL",
      "year": "2026",
      "numberOfDays": 12,
      "accruedDays": 3.0,
      "usedDays": 1.5,
      "remainingDays": 1.5
    }
  ]
}
```

---

### `GET /api/leave-allocations`

Báo cáo số dư phép toàn công ty.

**Auth:** `Admin`, `HR`  
**Query params:** `pageNumber`, `pageSize`, `searchTerm`

---

### `POST /api/leave-allocations/list`

Báo cáo số dư phép toàn công ty — phiên bản POST body (dùng cho Angular khi cần truyền nhiều filter hơn GET query string).

**Auth:** `Admin`, `HR`

**Request body:**

```json
{
  "pageNumber": 1,
  "pageSize": 20,
  "searchTerm": "Nguyen"
}
```

**Response `200`:** Dữ liệu paged giống `GET /api/leave-allocations`.

---

### `GET /api/leave-allocations/employee/{employeeId}`

Số dư phép của một nhân viên cụ thể.

**Auth:** Bất kỳ user đã đăng nhập

---

### `POST /api/leave-allocations`

Phân bổ ngày phép thủ công cho nhân viên.

**Auth:** `Admin`, `HR`

**Request body:**

```json
{
  "employeeId": "66f1234abc...",
  "leaveTypeId": "66e1001def...",
  "year": "2026",
  "numberOfDays": 14
}
```

---

### `DELETE /api/leave-allocations/{id}`

Thu hồi phân bổ nghỉ phép.

**Auth:** `Admin`

---

### `POST /api/leave-allocations/initialize/{year}`

Tự động khởi tạo phân bổ nghỉ phép cho tất cả nhân viên active trong năm.

**Auth:** `Admin`, `HR`  
**Path param:** `year` (e.g., `2026`)

---

### `POST /api/leave-allocations/carry-forward/{fromYear}`

Chuyển tiếp số ngày phép còn dư sang năm tiếp theo (cuối năm).

**Auth:** `Admin`  
**Path param:** `fromYear` (e.g., `2025`)

---

## 15. Payroll — Cycles

**Base path:** `/api/payroll-cycles`  
**Auth:** `Admin`, `HR` (toàn bộ module)

---

### `POST /api/payroll-cycles/generate`

Tạo chu kỳ lương cho một tháng (idempotent — nếu đã tồn tại thì trả về chu kỳ cũ).

> **Bắt buộc** gọi trước khi chạy `/api/payrolls/generate`.

**Request body:**

```json
{ "month": 3, "year": 2026 }
```

**Response `200`:**

```json
{
  "data": {
    "id": "66f2001abc...",
    "monthKey": "03-2026",
    "startDate": "01/03/2026",
    "endDate": "31/03/2026",
    "standardWorkingDays": 21,
    "publicHolidaysExcluded": 0,
    "weeklyDaysOffSnapshot": "0,6",
    "status": "Open"
  }
}
```

---

### `POST /api/payroll-cycles/bulk-generate`

Tạo hàng loạt chu kỳ cho tất cả tháng trong một năm.

**Request body:**

```json
{ "year": 2026 }
```

---

### `GET /api/payroll-cycles/year/{year}`

Danh sách chu kỳ lương trong một năm.

---

### `GET /api/payroll-cycles/{monthKey}`

Chi tiết một chu kỳ. `monthKey` format: `MM-YYYY` (e.g., `03-2026`).

---

### `POST /api/payroll-cycles/{monthKey}/close`

Đóng/chốt chu kỳ lương (không cho phép thay đổi sau khi đóng).

---

### `POST /api/payroll-cycles/{monthKey}/cancel`

Hủy chu kỳ lương.

---

## 16. Payroll — Public Holidays

**Base path:** `/api/public-holidays`  
**Auth:** `Admin`, `HR` (toàn bộ module)

---

| Method | Path | Mô tả |
|---|---|---|
| `GET` | `/api/public-holidays` | Tất cả ngày lễ (không phân trang, sắp xếp theo ngày) |
| `GET` | `/api/public-holidays/year/{year}` | Ngày lễ của một năm |
| `POST` | `/api/public-holidays` | Tạo ngày lễ mới |
| `PUT` | `/api/public-holidays/{id}` | Cập nhật ngày lễ |
| `DELETE` | `/api/public-holidays/{id}` | Xóa mềm ngày lễ |

**Request body tạo/cập nhật:**

```json
{
  "date": "2026-04-30T00:00:00Z",
  "name": "Ngày Giải phóng miền Nam",
  "isRecurringYearly": true,
  "note": "Nghỉ lễ theo quy định nhà nước"
}
```

---

## 17. Payroll — Payrolls

**Base path:** `/api/payrolls`

---

### `GET /api/payrolls/me`

Lịch sử lương của bản thân.

**Auth:** Bất kỳ user đã đăng nhập

---

### `GET /api/payrolls/{id}`

Chi tiết một bảng lương.

**Auth:** `Admin`, `HR`, `Employee` (owner kiểm tra trong handler)

---

### `GET /api/payrolls/{id}/pdf`

Tải xuống phiếu lương PDF.

**Auth:** `Admin`, `HR`, `Employee` (owner)  
**Response:** File PDF (`application/pdf`)

---

### `GET /api/payrolls/export`

Xuất Excel toàn bộ lương một tháng.

**Auth:** `Admin`, `HR`  
**Query params:** `month` (MM-YYYY)  
**Response:** File Excel (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`)

---

### `GET /api/payrolls`

Danh sách lương toàn công ty của một tháng.

**Auth:** `Admin`, `HR`  
**Query params:** `month` (MM-YYYY), `pageNumber`, `pageSize`

---

### `GET /api/payrolls/employee/{employeeId}`

Lịch sử lương của một nhân viên cụ thể.

**Auth:** `Admin`, `HR`, `Manager`

---

### `POST /api/payrolls/generate`

Tính toán lương cho tất cả nhân viên trong một chu kỳ.

**Auth:** `Admin`, `HR`

> **Bắt buộc:** Chu kỳ lương (`/api/payroll-cycles/generate`) phải tồn tại trước.  
> **Cảnh báo:** Nếu lương tháng đó đã tồn tại, thao tác này sẽ **tính lại**.

**Request body:**

```json
{ "month": "03-2026" }
```

**Response `200`:** Số lượng bảng lương đã tạo/cập nhật.

---

### `POST /api/payrolls/{id}/status`

Cập nhật trạng thái bảng lương.

**Auth:** `Admin`, `HR`

**Request body:**

```json
{
  "status": "Paid",
  "paidDate": "2026-03-31T00:00:00Z"
}
```

> `PayrollStatus`: `Draft` → `Calculated` → `Approved` → `Paid`

---

### `GET /api/payrolls/tax-report/{year}`

Báo cáo thuế TNCN (PIT) hàng năm cho tất cả nhân viên.

**Auth:** `Admin`, `HR`  
**Path param:** `year` (integer, e.g., `2026`)

**Response:** Bảng tổng hợp thu nhập, khấu trừ, thuế theo từng nhân viên.

---

## 18. Performance Management

**Base path:** `/api/performance`  
**Auth:** `Admin`, `HR`, `Manager` (toàn bộ module)

---

### `GET /api/performance/goals/{employeeId}`

Danh sách mục tiêu của một nhân viên.

---

### `POST /api/performance/goals`

Tạo mục tiêu mới.

**Request body:**

```json
{
  "employeeId": "66f1234abc...",
  "title": "Hoàn thiện module Payroll",
  "description": "Viết đủ unit test, coverage > 80%",
  "targetDate": "2026-06-30T00:00:00Z",
  "progress": 0,
  "status": "InProgress"
}
```

**Response `201`:** ID của goal vừa tạo.

---

### `PATCH /api/performance/goals/{id}/progress`

Cập nhật tiến độ mục tiêu (0–100).

**Request body:** Số thực (double) — không bọc object:

```json
75.5
```

> Khi `progress = 100`, status tự động chuyển thành `Completed`.

---

### `GET /api/performance/reviews/{employeeId}`

Danh sách đánh giá hiệu suất của một nhân viên.

---

### `POST /api/performance/reviews`

Tạo đánh giá hiệu suất.

**Request body:**

```json
{
  "employeeId": "66f1234abc...",
  "reviewerId": "66f9999xyz...",
  "periodStart": "2026-01-01T00:00:00Z",
  "periodEnd": "2026-03-31T00:00:00Z",
  "status": "Draft",
  "overallScore": 82.5,
  "notes": "Hoàn thành tốt các nhiệm vụ được giao..."
}
```

**Response `201`:** ID của review vừa tạo.

---

### `PATCH /api/performance/reviews/{id}`

Cập nhật đánh giá hiệu suất.

**Request body:** Tương tự create.

---

## 19. Notifications

**Base path:** `/api/notifications`  
**Auth:** Bất kỳ user đã đăng nhập

---

### `GET /api/notifications`

Danh sách thông báo của tôi.

**Query params:**

| Param | Type | Mô tả |
|---|---|---|
| `unreadOnly` | `bool` | `true` = chỉ lấy thông báo chưa đọc |

**Response `200`:**

```json
{
  "data": [
    {
      "id": "66n001abc...",
      "title": "Đơn nghỉ phép đã được duyệt",
      "body": "Đơn nghỉ phép ngày 10/03/2026 - 12/03/2026 đã được duyệt.",
      "type": "LeaveApproved",
      "isRead": false,
      "referenceId": "66l001def...",
      "referenceType": "leave_requests",
      "createdAt": "2026-03-06T10:15:00Z"
    }
  ]
}
```

---

### `GET /api/notifications/unread-count`

Số lượng thông báo chưa đọc (dùng cho badge trên navbar).

**Response `200`:**

```json
{ "data": 5 }
```

---

### `POST /api/notifications/{id}/read`

Đánh dấu một thông báo đã đọc.

---

### `POST /api/notifications/read-all`

Đánh dấu tất cả thông báo đã đọc.

---

## 20. Dashboard

**Base path:** `/api/dashboard`  
**Auth:** `Admin`, `HR`

---

### `GET /api/dashboard`

Lấy tổng hợp KPI từ các Provider (HR + Leave + Recruitment).

**Response `200`:**

```json
{
  "data": {
    "totalEmployees": 85,
    "activeEmployees": 82,
    "newHiresThisMonth": 3,
    "pendingLeaveRequests": 7,
    "totalLeaveRequestsThisMonth": 12,
    "openVacancies": 4,
    "candidatesThisMonth": 23,
    "interviewsScheduled": 8
  }
}
```

---

## 21. Audit Logs

**Base path:** `/api/auditlogs`  
**Auth:** `Admin`

---

### `GET /api/auditlogs` *(Offset-based)*

Danh sách audit log (phân trang offset — dùng cho tập nhỏ).

**Query params:**

| Param | Type | Mô tả |
|---|---|---|
| `pageNumber` | `int` | Trang hiện tại (default: `1`) |
| `pageSize` | `int` | Kích thước trang (default: `20`) |
| `searchTerm` | `string?` | Tìm theo action/entity |
| `startDate` | `DateTime?` | Từ ngày |
| `endDate` | `DateTime?` | Đến ngày |
| `userId` | `string?` | Lọc theo user |
| `actionType` | `string?` | Lọc theo action (`Create`, `Update`, `Delete`, `Login`) |

---

### `GET /api/auditlogs/cursor` *(Cursor-based — khuyến nghị)*

Phân trang cursor — hiệu quả cho collection 250K+ document (tránh `Skip(N)`).

**Query params:**

| Param | Type | Mô tả |
|---|---|---|
| `afterCursor` | `string?` | Cursor từ response trước (bỏ qua cho trang đầu) |
| `pageSize` | `int` | Default `20` |
| `searchTerm`, `startDate`, `endDate`, `userId`, `actionType` | | Tương tự offset |

**Response `200`:**

```json
{
  "data": {
    "items": [
      {
        "id": "66a001abc...",
        "userId": "a1b2c3d4-...",
        "userName": "admin@company.com",
        "action": "UPDATE",
        "tableName": "employees",
        "recordId": "66f1234abc...",
        "oldValues": "{\"fullName\":\"Old Name\"}",
        "newValues": "{\"fullName\":\"New Name\"}",
        "createdAt": "2026-03-06T09:30:00Z"
      }
    ],
    "nextCursor": "66a001abc...",
    "hasMore": true
  }
}
```

---

## 22. File Upload

**Base path:** `/api/files`  
**Auth:** Bất kỳ user đã đăng nhập  
**Rate limit:** `file-upload` (20 uploads/giờ/user)

---

### `POST /api/files/upload`

Upload file (avatar, CV, contract PDF, v.v.).

**Content-Type:** `multipart/form-data`

**Form fields:**

| Field | Type | Mô tả |
|---|---|---|
| `file` | `file` | File cần upload |
| `folder` | `string?` | Subfolder (e.g., `avatars`, `contracts`, `resumes`) |

**Response `200`:**

```json
{
  "data": {
    "url": "https://storage.example.com/employee-files/avatars/abc123.jpg",
    "fileName": "abc123.jpg",
    "size": 204800,
    "contentType": "image/jpeg"
  }
}
```

> **Storage backend:** Local disk (dev) hoặc Supabase Storage (prod).  
> **Max file size:** Cấu hình qua `appsettings.json`.

---

## 23. Dev Tools

**Base path:** `/api/dev`  
**Auth:** `Admin` (toàn bộ module)  
**Môi trường:** Chỉ hoạt động khi `ASPNETCORE_ENVIRONMENT = Development`. Route không được đăng ký trong Production.

---

### `POST /api/dev/seed-attendance`

Seed dữ liệu chấm công mẫu (Mon–Fri đủ công) cho tất cả nhân viên active trong một tháng.  
Bỏ qua nhân viên đã có bucket cho tháng đó.

**Auth:** `Admin`  
**Query params:** `month` (MM-yyyy, e.g., `02-2026`) — mặc định tháng trước nếu không truyền.

> **Chỉ dùng khi phát triển/test.** Endpoint này bị chặn hoàn toàn ở môi trường production (HTTP 403).

**Response `200`:**

```json
{
  "data": {
    "month": "02-2026",
    "created": 12,
    "skipped": 3,
    "totalEmployees": 15
  }
}
```

---

## 24. Rate Limiting

Hệ thống sử dụng `PartitionedRateLimiter` — mỗi user/IP có bucket độc lập.

| Policy | Áp dụng cho | Giới hạn |
|---|---|---|
| `auth` | `POST /api/auth/login` | 10 req/phút/IP |
| `refresh` | `POST /api/auth/refresh-token`, `POST /api/auth/logout` | Thoải mái hơn `auth` |
| `general` | Các endpoint API thông thường | 60 req/phút/user |
| `checkin` | `POST /api/attendance/check-in`, `check-out` | 10 lần/giờ/user |
| `write` | `POST /api/leaves`, `PATCH`, action endpoints | 30 mutations/phút/user |
| `file-upload` | `POST /api/files/upload` | 20 uploads/giờ/user |

**Khi bị chặn (429):**

```json
{
  "succeeded": false,
  "message": "Too many requests. Please wait before trying again.",
  "errorCode": "RATE_LIMIT_EXCEEDED"
}
```

Header `Retry-After: <seconds>` luôn có trong response 429.

---

## 25. Error Codes Reference

| Error Code | HTTP | Mô tả |
|---|---|---|
| `NOT_FOUND` | 404 | Resource không tồn tại |
| `VALIDATION_ERROR` | 400 | Input validation thất bại |
| `INVALID_DATA` | 400 | Dữ liệu không hợp lệ (logic) |
| `CONFLICT` | 409 | Trùng dữ liệu (duplicate code, email, ...) |
| `CONCURRENCY_CONFLICT` | 409 | Optimistic lock — `version` không khớp |
| `INVALID_STATE` | 409 | Trạng thái không cho phép thao tác |
| `UNAUTHORIZED` | 401 | Chưa đăng nhập hoặc token hết hạn |
| `FORBIDDEN` | 403 | Không đủ quyền role |
| `REFRESH_TOKEN_REQUIRED` | 401 | Cookie `refreshToken` thiếu hoặc hết hạn |
| `CANDIDATE_NOT_FOUND` | 404 | Ứng viên không tồn tại |
| `GOAL_NOT_FOUND` | 404 | Mục tiêu không tồn tại |
| `REVIEW_NOT_FOUND` | 404 | Đánh giá không tồn tại |
| `INVALID_YEAR` | 400 | Năm ngoài phạm vi cho phép (2000–2100) |
| `RATE_LIMIT_EXCEEDED` | 429 | Vượt giới hạn request |
| `UNLINKED_ACCOUNT` | 400 | Tài khoản chưa được liên kết với hồ sơ nhân viên |
| `INTERNAL_ERROR` | 500 | Lỗi server — kèm `correlationId` để trace |

---

## Appendix: Quick Reference — Tất cả Endpoints

| Method | Path | Auth | Mô tả |
|---|---|---|---|
| POST | `/api/auth/login` | Public | Đăng nhập |
| POST | `/api/auth/logout` | Public | Đăng xuất |
| POST | `/api/auth/refresh-token` | Public | Làm mới access token |
| POST | `/api/auth/forgot-password` | Public | Quên mật khẩu |
| POST | `/api/auth/reset-password` | Public | Đặt lại mật khẩu |
| POST | `/api/auth/change-password` | Any | Đổi mật khẩu |
| POST | `/api/auth/register` | Admin | Tạo tài khoản |
| GET | `/api/auth/users` | Admin, HR | Danh sách user |
| PATCH | `/api/auth/roles/{userId}` | Admin | Cập nhật roles user |
| POST | `/api/auth/status/{userId}` | Admin, HR | Kích hoạt/vô hiệu hóa user |
| GET | `/api/auth/roles` | Admin | Danh sách roles |
| POST | `/api/auth/role` | Admin | Tạo role |
| POST | `/api/auth/assign-role` | Admin | Gán role |
| GET | `/api/employees` | Any | Danh sách nhân viên |
| GET | `/api/employees/lookup` | Any | Dropdown nhân viên |
| GET | `/api/employees/org-chart` | Any | Sơ đồ tổ chức |
| GET | `/api/employees/{id}` | Any | Chi tiết nhân viên |
| POST | `/api/employees` | Admin, HR | Tạo nhân viên |
| PATCH | `/api/employees/{id}` | Admin, HR | Cập nhật nhân viên |
| DELETE | `/api/employees/{id}` | Admin | Xóa nhân viên |
| GET | `/api/contracts` | Admin, HR | Danh sách hợp đồng |
| GET | `/api/contracts/{id}` | Admin, HR | Chi tiết hợp đồng |
| GET | `/api/contracts/employee/{id}` | Admin, HR | HĐ của nhân viên |
| GET | `/api/contracts/me` | Any | HĐ của tôi |
| POST | `/api/contracts` | Admin, HR | Tạo hợp đồng |
| PATCH | `/api/contracts/{id}` | Admin, HR | Cập nhật hợp đồng |
| DELETE | `/api/contracts/{id}` | Admin | Xóa hợp đồng |
| GET | `/api/departments` | Any | Danh sách phòng ban |
| GET | `/api/departments/tree` | Any | Cây phòng ban |
| GET | `/api/departments/{id}` | Any | Chi tiết phòng ban |
| POST | `/api/departments` | Admin | Tạo phòng ban |
| PATCH | `/api/departments/{id}` | Admin | Cập nhật phòng ban |
| DELETE | `/api/departments/{id}` | Admin | Xóa phòng ban |
| GET | `/api/positions` | Any | Danh sách chức vụ |
| GET | `/api/positions/tree` | Any | Cây chức vụ |
| GET | `/api/positions/{id}` | Any | Chi tiết chức vụ |
| POST | `/api/positions` | Admin, HR | Tạo chức vụ |
| PATCH | `/api/positions/{id}` | Admin, HR | Cập nhật chức vụ |
| DELETE | `/api/positions/{id}` | Admin | Xóa chức vụ |
| GET | `/api/recruitment/vacancies` | Admin, HR | Danh sách tin tuyển dụng |
| GET | `/api/recruitment/vacancies/{id}` | Admin, HR | Chi tiết tin |
| POST | `/api/recruitment/vacancies` | Admin, HR | Tạo tin tuyển dụng |
| PATCH | `/api/recruitment/vacancies/{id}` | Admin, HR | Cập nhật tin |
| POST | `/api/recruitment/vacancies/{id}/close` | Admin, HR | Đóng tin |
| DELETE | `/api/recruitment/vacancies/{id}` | Admin, HR | Xóa tin |
| GET | `/api/recruitment/candidates` | Admin, HR | Danh sách ứng viên |
| GET | `/api/recruitment/candidates/{id}` | Admin, HR | Chi tiết ứng viên |
| POST | `/api/recruitment/candidates` | Admin, HR | Tạo ứng viên |
| PATCH | `/api/recruitment/candidates/{id}` | Admin, HR | Cập nhật ứng viên |
| POST | `/api/recruitment/candidates/{id}/status` | Admin, HR | Đổi trạng thái |
| POST | `/api/recruitment/candidates/{id}/onboard` | Admin, HR | Onboard nhân viên |
| DELETE | `/api/recruitment/candidates/{id}` | Admin, HR | Xóa ứng viên |
| GET | `/api/recruitment/interviews` | Admin, HR | Danh sách phỏng vấn |
| GET | `/api/recruitment/interviews/{id}` | Admin, HR | Chi tiết phỏng vấn |
| POST | `/api/recruitment/interviews` | Admin, HR | Tạo lịch phỏng vấn |
| PATCH | `/api/recruitment/interviews/{id}` | Admin, HR | Cập nhật lịch |
| POST | `/api/recruitment/interviews/{id}/review` | Admin, HR | Ghi kết quả |
| DELETE | `/api/recruitment/interviews/{id}` | Admin, HR | Xóa lịch phỏng vấn |
| POST | `/api/attendance/check-in` | Any | Chấm công vào |
| POST | `/api/attendance/check-out` | Any | Chấm công ra |
| GET | `/api/attendance/me/today-status` | Any | Trạng thái hôm nay |
| GET | `/api/attendance/me/range` | Any | Chấm công theo khoảng |
| GET | `/api/attendance/me/report` | Any | Báo cáo tháng của tôi |
| GET | `/api/attendance/team/summary` | Admin, HR, Mgr | Tổng hợp team |
| GET | `/api/attendance/employee/{id}/report` | Admin, HR, Mgr | BC nhân viên |
| GET | `/api/attendance/daily/{dateStr}` | Admin, HR, Mgr | BC ngày |
| POST | `/api/attendance/process-logs` | Admin, HR | Xử lý raw logs |
| POST | `/api/attendance/admin/force-reprocess` | Admin | Xử lý lại tháng |
| POST | `/api/attendance/admin/backfill-holidays` | Admin, HR | Cập nhật cờ ngày lễ |
| POST | `/api/attendance/explanation` | Any | Gửi đơn giải trình |
| GET | `/api/attendance/explanation/me` | Any | Đơn giải trình của tôi |
| GET | `/api/attendance/explanation/pending` | Admin, HR, Mgr | Đơn chờ duyệt |
| PUT | `/api/attendance/explanation/{id}/review` | Admin, HR, Mgr | Duyệt/từ chối giải trình |
| POST | `/api/attendance/overtime-schedule` | Admin, HR | Tạo OT schedule |
| POST | `/api/attendance/overtime-schedule/bulk` | Admin, HR | Tạo hàng loạt OT schedule |
| DELETE | `/api/attendance/overtime-schedule/{id}` | Admin, HR | Xóa OT schedule |
| GET | `/api/attendance/overtime-schedule` | Admin, HR, Mgr | Danh sách OT schedule |
| GET | `/api/shifts` | Any | Danh sách ca làm việc |
| GET | `/api/shifts/lookup` | Any | Dropdown ca làm việc |
| GET | `/api/shifts/{id}` | Any | Chi tiết ca làm việc |
| POST | `/api/shifts` | Admin, HR | Tạo ca làm việc |
| PATCH | `/api/shifts/{id}` | Admin, HR | Cập nhật ca làm việc |
| DELETE | `/api/shifts/{id}` | Admin, HR | Xóa ca làm việc |
| GET | `/api/leave-types` | Any | Danh sách loại nghỉ phép |
| GET | `/api/leave-types/{id}` | Any | Chi tiết loại nghỉ phép |
| POST | `/api/leave-types` | Admin | Tạo loại nghỉ phép |
| PATCH | `/api/leave-types/{id}` | Admin | Cập nhật loại nghỉ phép |
| DELETE | `/api/leave-types/{id}` | Admin | Xóa loại nghỉ phép |
| GET | `/api/leaves` | Admin, HR, Mgr | Tất cả đơn nghỉ |
| GET | `/api/leaves/me` | Any | Đơn nghỉ của tôi |
| GET | `/api/leaves/{id}` | Any | Chi tiết đơn nghỉ |
| GET | `/api/leaves/employee/{id}` | Admin, HR, Mgr | Đơn nghỉ nhân viên |
| POST | `/api/leaves` | Any | Tạo đơn nghỉ |
| PATCH | `/api/leaves/{id}` | Owner | Sửa đơn nghỉ |
| POST | `/api/leaves/{id}/cancel` | Owner | Hủy đơn nghỉ |
| POST | `/api/leaves/{id}/review` | Admin, HR, Mgr | Duyệt/từ chối |
| GET | `/api/leave-allocations/me` | Any | Số dư phép của tôi |
| GET | `/api/leave-allocations` | Admin, HR | Báo cáo số dư toàn CT |
| GET | `/api/leave-allocations/employee/{id}` | Any | Số dư phép nhân viên |
| POST | `/api/leave-allocations` | Admin, HR | Phân bổ ngày phép |
| DELETE | `/api/leave-allocations/{id}` | Admin | Thu hồi phân bổ |
| POST | `/api/leave-allocations/initialize/{year}` | Admin, HR | Khởi tạo năm mới |
| POST | `/api/leave-allocations/list` | Admin, HR | Danh sách số dư phép (POST body) |
| POST | `/api/leave-allocations/carry-forward/{year}` | Admin | Chuyển tiếp phép |
| POST | `/api/payroll-cycles/generate` | Admin, HR | Tạo chu kỳ lương |
| POST | `/api/payroll-cycles/bulk-generate` | Admin, HR | Tạo hàng loạt chu kỳ |
| GET | `/api/payroll-cycles/year/{year}` | Admin, HR | Chu kỳ theo năm |
| GET | `/api/payroll-cycles/{monthKey}` | Admin, HR | Chi tiết chu kỳ |
| POST | `/api/payroll-cycles/{monthKey}/close` | Admin, HR | Đóng chu kỳ |
| POST | `/api/payroll-cycles/{monthKey}/cancel` | Admin, HR | Hủy chu kỳ |
| GET | `/api/public-holidays` | Admin, HR | Tất cả ngày lễ |
| GET | `/api/public-holidays/year/{year}` | Admin, HR | Ngày lễ theo năm |
| POST | `/api/public-holidays` | Admin, HR | Tạo ngày lễ |
| PUT | `/api/public-holidays/{id}` | Admin, HR | Cập nhật ngày lễ |
| DELETE | `/api/public-holidays/{id}` | Admin, HR | Xóa ngày lễ |
| GET | `/api/payrolls/me` | Any | Lịch sử lương của tôi |
| GET | `/api/payrolls/{id}` | Admin, HR, Emp | Chi tiết bảng lương |
| GET | `/api/payrolls/{id}/pdf` | Admin, HR, Emp | Tải PDF phiếu lương |
| GET | `/api/payrolls/export` | Admin, HR | Xuất Excel lương |
| GET | `/api/payrolls` | Admin, HR | Danh sách lương tháng |
| GET | `/api/payrolls/employee/{id}` | Admin, HR, Mgr | Lịch sử lương NV |
| POST | `/api/payrolls/generate` | Admin, HR | Tính lương tháng |
| POST | `/api/payrolls/{id}/status` | Admin, HR | Cập nhật trạng thái |
| GET | `/api/payrolls/tax-report/{year}` | Admin, HR | Báo cáo thuế TNCN |
| GET | `/api/performance/goals/{employeeId}` | Admin, HR, Mgr | Danh sách mục tiêu |
| POST | `/api/performance/goals` | Admin, HR, Mgr | Tạo mục tiêu |
| PATCH | `/api/performance/goals/{id}/progress` | Admin, HR, Mgr | Cập nhật tiến độ |
| GET | `/api/performance/reviews/{employeeId}` | Admin, HR, Mgr | Danh sách đánh giá |
| POST | `/api/performance/reviews` | Admin, HR, Mgr | Tạo đánh giá |
| PATCH | `/api/performance/reviews/{id}` | Admin, HR, Mgr | Cập nhật đánh giá |
| GET | `/api/notifications` | Any | Thông báo của tôi |
| GET | `/api/notifications/unread-count` | Any | Số thông báo chưa đọc |
| POST | `/api/notifications/{id}/read` | Any | Đánh dấu đã đọc |
| POST | `/api/notifications/read-all` | Any | Đánh dấu tất cả đã đọc |
| GET | `/api/dashboard` | Admin, HR | KPI dashboard |
| GET | `/api/auditlogs` | Admin | Audit logs (offset) |
| GET | `/api/auditlogs/cursor` | Admin | Audit logs (cursor) |
| POST | `/api/files/upload` | Any | Upload file |
| POST | `/api/dev/seed-attendance` | Admin (Dev only) | Seed dữ liệu chấm công mẫu |
