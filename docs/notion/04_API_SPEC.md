# 🔌 API Specification — Đặc Tả API

**Base URL**: `http://localhost:5000/api`
**Auth**: JWT Bearer Token (header: `Authorization: Bearer <token>`)
**Format**: JSON
**Response**: `{ success: bool, data: T, message: string }`

---

## 🔐 Authentication — /api/auth

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | /login | ❌ Public | — | Đăng nhập, nhận JWT token |
| 2 | POST | /register | ✅ | Admin | Tạo tài khoản mới |
| 3 | POST | /role | ✅ | Admin | Tạo role mới |
| 4 | POST | /assign-role | ✅ | Admin | Gán role cho user |
| 5 | GET | /users | ✅ | Admin, HR | Danh sách users (paged) |
| 6 | PUT | /roles/{userId} | ✅ | Admin | Cập nhật roles của user |
| 7 | PUT | /status/{userId} | ✅ | Admin, HR | Khóa/Mở khóa user |
| 8 | POST | /change-password | ✅ | All | Đổi mật khẩu |
| 9 | GET | /roles | ✅ | Admin | Lấy tất cả roles |

---

## 👤 Employees — /api/employees

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | /list | ✅ | All | Danh sách NV (paged) |
| 2 | GET | /lookup | ✅ | All | Lookup NV (dropdown) |
| 3 | GET | /org-chart | ✅ | All | Sơ đồ tổ chức |
| 4 | GET | /{id} | ✅ | All | Chi tiết NV |
| 5 | POST | / | ✅ | Admin, HR | Tạo NV mới |
| 6 | PUT | /{id} | ✅ | Admin, HR | Cập nhật NV |
| 7 | DELETE | /{id} | ✅ | Admin | Xóa NV |

---

## 📄 Contracts — /api/contracts

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | /list | ✅ | Admin, HR | Danh sách HĐ (paged) |
| 2 | GET | /{id} | ✅ | Admin, HR | Chi tiết HĐ |
| 3 | GET | /employee/{empId} | ✅ | All | HĐ theo NV |
| 4 | POST | / | ✅ | Admin, HR | Tạo HĐ mới |
| 5 | PUT | /{id} | ✅ | Admin, HR | Cập nhật HĐ |
| 6 | PUT | /{id}/terminate | ✅ | Admin, HR | Chấm dứt HĐ |

---

## ⏰ Attendance — /api/attendance

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | /check-in | ✅ | All | Chấm công vào |
| 2 | POST | /check-out | ✅ | All | Chấm công ra |
| 3 | GET | /me/range | ✅ | All | Công cá nhân theo khoảng |
| 4 | GET | /me/report | ✅ | All | Báo cáo công tháng |
| 5 | GET | /team/summary | ✅ | Admin, HR, Mgr | Tổng hợp công team |
| 6 | GET | /employee/{id}/report | ✅ | Admin, HR, Mgr | Công NV cụ thể |
| 7 | GET | /daily/{dateStr} | ✅ | Admin, HR, Mgr | Báo cáo ngày |
| 8 | POST | /process-logs | ✅ | Admin, HR | Xử lý log chấm công |

---

## 🌴 Leave — /api/leaves

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | /list | ✅ | Admin, HR, Mgr | DS đơn nghỉ (paged) |
| 2 | GET | /me | ✅ | All | DS đơn nghỉ cá nhân |
| 3 | GET | /{id} | ✅ | All | Chi tiết đơn |
| 4 | POST | / | ✅ | All | Tạo đơn xin phép |
| 5 | PUT | /{id} | ✅ | All | Sửa đơn |
| 6 | PUT | /{id}/cancel | ✅ | All | Hủy đơn |
| 7 | PUT | /{id}/review | ✅ | Admin, HR, Mgr | Duyệt/Từ chối đơn |

### Leave Types — /api/leave-types

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | GET | / | ✅ | All | DS loại phép |
| 2 | POST | / | ✅ | Admin, HR | Tạo loại phép |

### Leave Allocations — /api/leave-allocations

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | GET | /me | ✅ | All | Số dư phép cá nhân |
| 2 | GET | /employee/{id} | ✅ | Admin, HR | Phép của NV |
| 3 | POST | /initialize | ✅ | Admin, HR | Khởi tạo phép năm |
| 4 | POST | /accrue | ✅ | Admin | Cộng dồn thủ công |

---

## 💰 Payroll — /api/payrolls

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | GET | /me | ✅ | All | Lịch sử lương cá nhân |
| 2 | GET | /{id} | ✅ | All | Chi tiết phiếu lương |
| 3 | GET | / | ✅ | Admin, HR | Bảng lương tháng |
| 4 | POST | /generate | ✅ | Admin, HR | Tính lương |
| 5 | PUT | /{id}/status | ✅ | Admin, HR | Duyệt/Xác nhận lương |

---

## 🏢 Departments — /api/departments

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | /list | ✅ | All | DS phòng ban (paged) |
| 2 | GET | /{id} | ✅ | All | Chi tiết |
| 3 | GET | /tree | ✅ | All | Cây phòng ban |
| 4 | POST | / | ✅ | Admin, HR | Tạo |
| 5 | PUT | /{id} | ✅ | Admin, HR | Cập nhật |
| 6 | DELETE | /{id} | ✅ | Admin, HR | Xóa |

---

## 🏢 Positions — /api/positions

| # | Method | Path | Auth | Role | Description |
|---|--------|------|------|------|-------------|
| 1 | POST | /list | ✅ | All | DS chức vụ (paged) |
| 2 | GET | /{id} | ✅ | All | Chi tiết |
| 3 | GET | /tree | ✅ | All | Cây chức vụ (optional `departmentId`) |
| 4 | POST | / | ✅ | Admin, HR | Tạo (requires `departmentId`) |
| 5 | PUT | /{id} | ✅ | Admin, HR | Cập nhật |
| 6 | DELETE | /{id} | ✅ | Admin, HR | Xóa |

---

## 🔄 Request Flow

Client Request → JWT Auth → Role Check → Validation Filter (FluentValidation) → Handler → Service → Response
