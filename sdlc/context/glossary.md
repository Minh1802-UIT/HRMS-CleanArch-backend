# HRMS Glossary

## Core Entities

### Employee
- Basic information (name, email, phone)
- Employment details (department, position, start date)
- Status (active, inactive, terminated)

### Leave
- Types: Annual, Sick, Personal, Maternity, Paternity
- Status: Pending, Approved, Rejected, Cancelled
- Balance tracking

### Attendance
- Check-in / Check-out times
- Overtime tracking
- Late arrivals

### Payroll
- Salary components: Basic, Allowances, Deductions
- Pay period: Monthly
- Tax calculations

## Technical Terms

| Term | Definition |
|------|------------|
| CQRS | Command Query Responsibility Segregation |
| MediatR | Mediator pattern implementation for .NET |
| FluentValidation | Fluent validation library |
| Clean Architecture | Layered architecture pattern |
| ADR | Architectural Decision Record |
| SPEC.md | Feature specification document |
| REQ-XXX | Requirement identifier |

## Project Structure

```
HRMS-CleanArch-backend/
â”œâ”€â”€ Domain/           # Core business entities
â”œâ”€â”€ Application/      # Use cases, CQRS
â”œâ”€â”€ Infrastructure/   # Data access, external services
â”œâ”€â”€ API/              # REST endpoints
â””â”€â”€ Tests/            # Unit & integration tests

HRMS-CleanArch-frontend/
â”œâ”€â”€ src/app/
â”‚   â”œâ”€â”€ features/     # Feature modules
â”‚   â”œâ”€â”€ components/   # Shared components
â”‚   â”œâ”€â”€ services/     # HTTP services
â”‚   â””â”€â”€ models/       # TypeScript interfaces
```
