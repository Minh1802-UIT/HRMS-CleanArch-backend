---
name: hrms-fullstack
description: Tạo feature HRMS full-stack từ spec đến implementation. Bao gồm viết SPEC.md, tạo CQRS command/query backend, API endpoint, và Angular component/ page frontend. Sử dụng khi user yêu cầu tạo feature mới, thêm API, hoặc làm việc với HRMS module.
---

# HRMS Full-Stack Feature Development

## Workflow

Khi tạo feature mới:

### 1. Đọc Spec
- Kiểm tra `sdlc/specs/REQ-XXX/SPEC.md` đã tồn tại chưa
- Nếu chưa, tạo mới theo template `sdlc/specs/REQ-001-sample-feature/SPEC.md`
- Đọc decisions trong `sdlc/specs/REQ-XXX/decisions/` nếu có

### 2. Backend Implementation

**CQRS Pattern (MediatR):**
- Command: `Application/Commands/FeatureName/`
- Query: `Application/Queries/FeatureName/`
- Handler đặt cùng folder với command/query
- Validator: FluentValidation trong folder `Application/Validators/`

**API Layer:**
- Controller trong `API/Controllers/`
- Endpoint versioning qua `[ApiVersion]`
- Response bọc trong `ApiResponse<T>`

**Domain:**
- Entity trong `Domain/Entities/`
- Interface repository trong `Domain/Interfaces/`
- DTO trong `Application/DTOs/`

### 3. Frontend Implementation

**Angular 17+:**
- Standalone components trong `src/app/features/`
- Lazy route: `loadComponent: () => import(...)`
- Service gọi API trong `src/app/core/services/`
- DTO/interface trong `src/app/models/`

**State:**
- Ưu tiên Signals (`signal()`, `computed()`, `effect()`)
- Tránh trùng lặp với BehaviorSubject

**UI:**
- PrimeNG components
- Tailwind CSS cho styling

### 4. Validate

- Backend: chạy `dotnet build`, `dotnet test`
- Frontend: chạy `ng build`
- Verify API hoạt động qua Swagger

## Cấu trúc Folder Reference

```
Backend:
Employee.API/Controllers/
Employee.Application/
  Commands/FeatureName/
  Queries/FeatureName/
  Validators/
  DTOs/
Employee.Domain/
  Entities/
  Interfaces/
Employee.Infrastructure/
  Data/Repositories/

Frontend:
src/app/
  core/services/
  features/module-name/
  models/
  shared/
```

## Quick Command

- Tạo spec mới: copy từ `sdlc/specs/REQ-001-sample-feature/SPEC.md`
- Tạo backend CQRS: dùng MediatR pattern, FluentValidation
- Tạo frontend: standalone component, lazy load
