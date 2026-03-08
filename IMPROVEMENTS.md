# HRMS Project - Improvements & Follow-up Notes

> Last updated: March 7, 2026
> Project: HRMS-CleanArch (Backend .NET 8 + Frontend Angular 17)

---

## 🚨 High Priority

### Frontend

#### 1. Consolidate State Management
**Issue:** Using both Angular Signals AND RxJS BehaviorSubject creates redundancy and potential inconsistency.

**Current state:**
- `AuthService` maintains both `currentUserSignal` and `currentUserSubject`
- `MasterDataService` uses both patterns
- Dual state creates sync issues

**Status:** ✅ FIXED

**Solution applied:**
- Removed `BehaviorSubject` from `AuthService`, using `Signal` only
- Removed `BehaviorSubject` from `MasterDataService`, using `Signal` only
- Added backward-compatible Observable methods (`getDepartments$()` etc.) that internally use Signals for smooth migration

**Files updated:**
- `src/app/core/services/auth.service.ts`
- `src/app/features/organization/services/master-data.service.ts`

---

#### 2. Handle API Response Inconsistency
**Issue:** Backend returns mixed casing (`departmentName`, `DepartmentName`), frontend has fallbacks like:
```typescript
employee.departmentName || employee.DepartmentName
```

**Status:** ✅ FIXED

**Solution applied:**
- Created `dto-normalizer.ts` utility in `src/app/shared/utils/`
- Provides `normalizeDto<T>()` function to convert all properties to camelCase
- Includes `mapDto<TInput, TOutput>()` for type-safe transformations
- Services can now use: `normalizeApiResponse(response)` or `mapDto(rawData, mapper)`

**Files created:**
- `src/app/shared/utils/dto-normalizer.ts`

**Usage example:**
```typescript
import { normalizeDto, mapDto } from '@shared/utils/dto-normalizer';

// Option 1: Simple normalization
const normalized = normalizeDto(apiResponse.data);

// Option 2: Type-safe mapping
const employee = mapDto(rawData, (d) => ({
  id: d.id,
  name: d.fullName,  // Now always camelCase
  departmentName: d.departmentName  // No more fallback needed
}));
```

---

#### 3. Add Global Error User Feedback
**Issue:** Error interceptor only logs errors, doesn't show user-facing messages.

**Status:** ✅ FIXED

**Solution applied:**
- Error interceptor đã tích hợp với ToastService
- Convert ToastService từ BehaviorSubject sang Signal
- Convert NotificationToastComponent sang sử dụng Signals + control flow (@for)

**Files updated:**
- `src/app/core/services/toast.service.ts`
- `src/app/shared/components/notification-toast/notification-toast.component.ts`
- `src/app/shared/components/notification-toast/notification-toast.component.html`

---

### Backend

#### 4. Add Redis Health Check
**Issue:** Only MongoDB health check is implemented.

**Status:** ✅ FIXED

**Solution applied:**
- Added Redis health check in Program.cs
- Both MongoDB and Redis health checks are configured
- Failure status set to Unhealthy for both

**Code:**
```csharp
builder.Services.AddHealthChecks()
    .AddMongoDb(name: "mongodb", ...)
    .AddRedis(redisConnectionString, name: "redis", ...);
```

---

## ⚠️ Medium Priority

### Frontend

#### 5. Break Large Components
**Issue:** Some components exceed 300 lines:
- `EmployeeListComponent` (342 lines)
- `SidebarComponent` (245 lines)

**Status:** ✅ FIXED (Phase 2)
z
**Solution applied:**
- Extracted `EmployeeVirtualListComponent` for virtual scrolling
- Extracted `EmployeePaginationComponent` for pagination
- Extracted `EmployeeFiltersComponent` for filtering
- Extracted `EmployeeTableComponent` for table logic
- EmployeeListComponent reduced to 233 lines (from 342)
- SidebarComponent reduced to 143 lines (from 245)

---

#### 6. Standardize Import Paths
**Issue:** Mixed usage of path aliases and relative imports.

**Status:** 🔄 Partially Done

**Progress:**
- ✅ Configured path aliases in `tsconfig.json`: `@core/*`, `@features/*`, `@shared/*`, `@layout/*`, `@env/*`
- ✅ Updated `auth.service.ts` to use path aliases
- ⏳ Remaining: ~30 files cần update (manual review required)

**Recommended approach:**
- Standardize on path aliases: `@core/*`, `@features/*`, `@shared/*`
- Configure `tsconfig.json` paths consistently

---

#### 7. Add Loading States Consistently
**Issue:** Some async operations don't show loading indicators.

**Status:** ✅ FIXED

**Solution applied:**
- Created reusable `LoadingSpinnerComponent` in `src/app/shared/components/loading-spinner/`
- Supports 3 sizes: sm, md, lg
- Supports full-page overlay mode
- Custom message support
- Uses Tailwind CSS + CSS animations

**Files created:**
- `src/app/shared/components/loading-spinner/loading-spinner.component.ts`

**Usage example:**
```typescript
// Simple spinner
<app-loading-spinner />

// With custom size
<app-loading-spinner size="lg" />

// With message
<app-loading-spinner message="Loading employees..." />

// Full page overlay
<app-loading-spinner fullPage message="Saving..." />
```

---

### Backend

#### 8. Upgrade MongoDB Driver
**Issue:** Using `MongoDB.Driver 3.6.0` - relatively old version.

**Recommended approach:**
- Upgrade to 2.x+ for better performance and features
- Test thoroughly after upgrade

---

#### 9. Increase Test Coverage
**Issue:** Unit tests exist but coverage needs expansion.

**Recommended approach:**
- Add tests for critical services:
  - `TokenService`
  - `AuthService`
  - Payroll calculation services
  - Background jobs
- Add integration tests for API endpoints

---

## 🔧 Low Priority

### Frontend

- [x] Add API versioning strategy (consider `/api/v1/`)
- [x] Implement virtual scrolling for large lists
- [x] Add skeleton loaders for all data-fetching components
- [x] Consider adding unit tests for guards and interceptors

### Backend

- [x] Consider adding OpenTelemetry for distributed tracing
- [x] Add input sanitization for file uploads
- [x] Document API with OpenAPI/Swagger more thoroughly
- [x] Add integration tests for background jobs

---

## 📋 Quick Wins

### Frontend (1-2 hours each)

| Task | Effort | Impact | Status |
|------|--------|--------|--------|
| Add error toast for 500 errors | 1h | High | ✅ Done |
| Normalize API response DTOs | 2h | Medium | ✅ Done |
| Add loading spinner to Payroll page | 1h | Medium | ✅ Done |

### Backend (1-2 hours each)

| Task | Effort | Impact | Status |
|------|--------|--------|--------|
| Add Redis health check | 1h | Medium | ✅ Done |
| Add more unit tests | 2h | Medium | ✅ Done |
| Document API endpoints | 2h | Low | ✅ Done |

---

## 🎯 Roadmap Suggestions

### Phase 1: Stabilization (Week 1-2)
- [x] Fix state management hybrid issue
- [x] Add global error handling
- [x] Add Redis health check

### Phase 2: Performance (Week 3-4)
- [x] Upgrade MongoDB driver (3.6.0 → 3.4.2)
- [x] Break large components (EmployeeListComponent, SidebarComponent)
- [x] Add virtual scrolling (EmployeeVirtualListComponent with CDK)

### Phase 3: Quality (Week 5-6)
- [x] Increase test coverage (TokenService tests - 11 tests)
- [x] Add OpenTelemetry (Tracing with AspNetCore + HttpClient instrumentation)
- [x] Full API documentation (Swagger with XML comments, JWT auth, contact info)

---

## 📚 Resources

### Backend
- Clean Architecture: https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure-common/#clean-aspnet-core-mvc-architecture
- MediatR: https://github.com/jbogard/MediatR
- MongoDB Driver: https://mongodb.github.io/mongo-csharp-driver/

### Frontend
- Angular Signals: https://angular.io/guide/signals
- Standalone Components: https://angular.io/guide/standalone-components
- PrimeNG: https://primeng.org/

---

*This document serves as a living guide for project improvements.*
*Review and update quarterly.*
