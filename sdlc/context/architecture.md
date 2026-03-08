# HRMS Architecture Overview

## System Context

HRMS (Human Resource Management System) is a full-stack web application for managing HR processes.

## Tech Stack

### Backend
- .NET 8
- Clean Architecture
- MediatR (CQRS)
- FluentValidation
- Entity Framework Core
- SQL Server

### Frontend
- Angular 17+
- PrimeNG UI
- Tailwind CSS
- Signals for state

## Architecture Layers

```
+-------------------------------+
|          API Layer            |
|  (Controllers, Middleware)   |
+-------------------------------+
|       Application Layer       |
| (Commands, Queries, Handlers) |
+-------------------------------+
|          Domain Layer         |
|   (Entities, Value Objects)  |
+-------------------------------+
|      Infrastructure Layer    |
|   (EF Core, Repositories)    |
+-------------------------------+
```

## Key Modules

1. **Employee Management**: CRUD operations for employees
2. **Leave Management**: Leave requests, approvals
3. **Payroll**: Salary calculations
4. **Attendance**: Time tracking
5. **Reports**: Analytics and dashboards

## API Design

- RESTful APIs
- JSON responses
- Bearer token authentication
- Response wrapper: ApiResponse<T>
