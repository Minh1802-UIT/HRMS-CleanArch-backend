---
name: hrms-review
description: Code review cho HRMS theo coding standards. Kiem tra Clean Architecture, CQRS pattern, naming conventions, error handling. Su dung khi user yeu cau review code.
---

# HRMS Code Review

## Review Checklist

### Backend
- Clean Architecture: Domain -> Application -> Infrastructure -> API
- CQRS: Command/Query tach biet, khong mix trong cung handler
- Validation: dung FluentValidation, khong validate thu cong trong code
- Error Handling: try-catch, middleware xu ly exception
- Naming: PascalCase cho class/method, camelCase cho property
- API Response: wrapped trong ApiResponse<T>

### Frontend
- Standalone components
- Signals: dung signal() thay vi BehaviorSubject neu co the
- Async: dung async pipe hoac toSignal()
- Error handling: hien thi loi cho nguoi dung
- UI: PrimeNG components, Tailwind cho custom styles

### Chung
- Khong hardcode secrets, dung configuration
- Unit tests cho business logic
- Clean code: function ngan gon, tranh long

## Output
- List cac van de tim thay
- De xuat cach sua
- Review comments truc tiep tren code (neu co quyen)
