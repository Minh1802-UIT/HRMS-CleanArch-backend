# API SPECIFICATION — ĐẶC TẢ API

> Chi tiết tất cả REST API Endpoints, phân theo module.

---

## Conventions

| Item | Value |
|------|-------|
| **Base URL** | `http://localhost:5000/api` |
| **Auth** | JWT Bearer Token (header: `Authorization: Bearer <token>`) |
| **Format** | JSON |
| **Response** | `{ success: bool, data: T, message: string }` |

---

## 1. AUTHENTICATION — `/api/auth`

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | `/login` | ❌ Public | — | Đăng nhập, nhận JWT token |
| 2 | POST | `/register` | ✅ | Admin | Tạo tài khoản mới |
| 3 | POST | `/role` | ✅ | Admin | Tạo role mới |
| 4 | POST | `/assign-role` | ✅ | Admin | Gán role cho user |
| 5 | GET | `/users` | ✅ | Admin, HR | Danh sách users (paged) |
| 6 | PUT | `/roles/{userId}` | ✅ | Admin | Cập nhật roles của user |
| 7 | PUT | `/status/{userId}` | ✅ | Admin, HR | Khóa/Mở khóa user |
| 8 | POST | `/change-password` | ✅ | All | Đổi mật khẩu |
| 9 | GET | `/roles` | ✅ | Admin | Lấy danh sách tất cả roles |

### Request/Response Details

**POST /login**
```json
// Request
{ "username": "admin", "password": "<password-from-config>" }
// Response  
{ "token": "eyJ...", "fullName": "Admin", "roles": ["Admin"], "employeeId": "..." }
```

**POST /register**
```json
{ "username": "NV001", "email": "a@b.com", "password": "Pass@123", "fullName": "Nguyễn Văn A", "employeeId": "..." }
```

---

## 2. EMPLOYEES — `/api/employees`

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | `/list` | ✅ | All | Danh sách NV (paged) |
| 2 | GET | `/lookup` | ✅ | All | Lookup NV (dropdown) |
| 3 | GET | `/org-chart` | ✅ | All | Sơ đồ tổ chức |
| 4 | GET | `/{id}` | ✅ | All | Chi tiết NV |
| 5 | POST | `/` | ✅ | Admin, HR | Tạo NV mới |
| 6 | PUT | `/{id}` | ✅ | Admin, HR | Cập nhật NV |
| 7 | DELETE | `/{id}` | ✅ | Admin | Xóa NV |

---

## 3. CONTRACTS — `/api/contracts`

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | `/list` | ✅ | Admin, HR | Danh sách HĐ (paged) |
| 2 | GET | `/{id}` | ✅ | Admin, HR | Chi tiết HĐ |
| 3 | GET | `/employee/{empId}` | ✅ | All | HĐ theo NV |
| 4 | POST | `/` | ✅ | Admin, HR | Tạo HĐ mới |
| 5 | PUT | `/{id}` | ✅ | Admin, HR | Cập nhật HĐ |
| 6 | PUT | `/{id}/terminate` | ✅ | Admin, HR | Chấm dứt HĐ |

---

## 4. ATTENDANCE — `/api/attendance`

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | `/check-in` | ✅ | All | Chấm công vào |
| 2 | POST | `/check-out` | ✅ | All | Chấm công ra |
| 3 | GET | `/me/range?from=&to=` | ✅ | All | Công cá nhân theo khoảng |
| 4 | GET | `/me/report?month=` | ✅ | All | Báo cáo công tháng cá nhân |
| 5 | GET | `/team/summary?from=&to=` | ✅ | Admin, HR, Mgr | Tổng hợp công team |
| 6 | GET | `/employee/{id}/report` | ✅ | Admin, HR, Mgr | Công của NV cụ thể |
| 7 | GET | `/daily/{dateStr}` | ✅ | Admin, HR, Mgr | Báo cáo ngày (dashboard) |
| 8 | POST | `/process-logs` | ✅ | Admin, HR | Kích hoạt xử lý log |

---

## 5. LEAVE — `/api/leaves`

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | `/list` | ✅ | Admin, HR, Mgr | DS đơn nghỉ (paged) |
| 2 | GET | `/me` | ✅ | All | DS đơn nghỉ cá nhân |
| 3 | GET | `/{id}` | ✅ | All | Chi tiết đơn |
| 4 | POST | `/` | ✅ | All | Tạo đơn xin phép |
| 5 | PUT | `/{id}` | ✅ | All | Sửa đơn |
| 6 | PUT | `/{id}/cancel` | ✅ | All | Hủy đơn |
| 7 | PUT | `/{id}/review` | ✅ | Admin, HR, Mgr | Duyệt/Từ chối đơn |

### Leave Type APIs — `/api/leave-types`
| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | GET | `/` | ✅ | All | DS loại phép |
| 2 | POST | `/` | ✅ | Admin, HR | Tạo loại phép |

### Leave Allocation APIs — `/api/leave-allocations`
| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | GET | `/me` | ✅ | All | Số dư phép cá nhân |
| 2 | GET | `/employee/{id}` | ✅ | Admin, HR | Phép của NV |
| 3 | POST | `/initialize` | ✅ | Admin, HR | Khởi tạo phép năm |
| 4 | POST | `/accrue` | ✅ | Admin | Chạy cộng dồn thủ công |

---

## 6. PAYROLL — `/api/payrolls`

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | GET | `/me` | ✅ | All | Lịch sử lương cá nhân |
| 2 | GET | `/{id}` | ✅ | All | Chi tiết phiếu lương |
| 3 | GET | `/?month=&year=` | ✅ | Admin, HR | Bảng lương tháng (toàn cty) |
| 4 | POST | `/generate` | ✅ | Admin, HR | Tính lương |
| 5 | PUT | `/{id}/status` | ✅ | Admin, HR | Duyệt/Xác nhận lương |

---

## 7. ORGANIZATION

### Departments — `/api/departments`
| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | `/list` | ✅ | All | DS phòng ban (paged) |
| 2 | GET | `/{id}` | ✅ | All | Chi tiết phòng ban |
| 3 | GET | `/tree` | ✅ | All | Cây phòng ban |
| 4 | POST | `/` | ✅ | Admin, HR | Tạo phòng ban |
| 5 | PUT | `/{id}` | ✅ | Admin, HR | Cập nhật |
| 6 | DELETE | `/{id}` | ✅ | Admin, HR | Xóa |

### Positions — `/api/positions`
| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | `/list` | ✅ | All | DS chức vụ (paged) |
| 2 | GET | `/{id}` | ✅ | All | Chi tiết |
| 3 | GET | `/tree` | ✅ | All | Cây chức vụ (optional `departmentId`) |
| 4 | POST | `/` | ✅ | Admin, HR | Tạo (requires `departmentId`) |
| 5 | PUT | `/{id}` | ✅ | Admin, HR | Cập nhật |
| 6 | DELETE | `/{id}` | ✅ | Admin, HR | Xóa |

---

## 8. VALIDATION

Tất cả endpoints sử dụng `ValidationFilter<TDto>` (FluentValidation) để validate input tự động trước khi xử lý logic.

**Request Flow**:
```
Client Request → JWT Auth → Role Check → Validation Filter → Handler → Service → Response
```
