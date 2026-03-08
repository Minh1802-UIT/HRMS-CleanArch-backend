# PROJECTS

---

## HRMS - Human Resource Management System
**Full-stack Web Application | Enterprise HR Solution**

### Project Overview
A comprehensive HR management system built from scratch as a portfolio project, featuring complete HR workflows from recruitment to payroll processing. Designed to support medium-to-large organizations with 12+ integrated HR modules.

**Role:** Full-stack Developer (Individual Project)  
**Date Completed:** Ongoing (Started 2024, Active Development)  
**Tech Stack:** .NET 8, Angular 17, MongoDB, Redis, Docker  
**Demo:** https://hrms-clean-arch-frontend.vercel.app  
**API Docs:** https://hrms-api.onrender.com/swagger  
**GitHub:** https://github.com/Minh1802-UIT/EmployeeCleanArch

### Scope
- **Development Timeline:** 12+ months of continuous development
- **Team Size:** 1 developer (solo project)
- **Architecture:** Clean Architecture with CQRS pattern
- **Database:** MongoDB with 23 collections
- **Infrastructure:** Docker containerization, CI/CD pipelines
- **Deployment:** Render (Backend), Vercel (Frontend), MongoDB Atlas

### Features Implemented

#### Core Modules
- **Authentication & Security** - JWT + Refresh Token, token rotation, rate limiting, RBAC
- **Employee Management** - Full CRUD, org chart, soft delete, optimistic concurrency
- **Contract Management** - Fixed-term & indefinite contracts, salary components
- **Organization** - Hierarchical departments, positions with salary ranges
- **Attendance** - Real-time check-in/out with GPS (Leaflet), overtime calculation
- **Shift Management** - Multiple shifts, grace period, overnight support
- **Leave Management** - Leave types, accrual, sandwich rule, approval workflow
- **Payroll** - Monthly generation, Vietnamese PIT (7-tier), BHXH/BHYT/BHTN deductions
- **Payroll Cycles** - Working day snapshots, public holiday exclusion
- **Recruitment** - Job vacancies, candidate pipeline (Applied → Hired), onboarding
- **Performance** - Goal tracking, period reviews with scoring
- **Notifications & Dashboard** - In-app feed, KPI analytics, audit logs

#### Technical Features
- 19 Carter API endpoints with full CRUD operations
- 5 Background Services (Hangfire): Attendance, Leave Accrual, Payroll, Contracts, Cleanup
- PDF payslip generation (QuestPDF)
- Excel bulk export (ClosedXML)
- File uploads via Supabase Storage
- Swagger API documentation

### Measurable Results
- ✅ Production deployment with live demo
- ✅ 12 integrated HR modules covering full employee lifecycle
- ✅ Enterprise-grade security implementation
- ✅ Automated background job processing
- ✅ Responsive UI with modern Angular 17 + Signals
- ✅ Comprehensive test coverage with unit + integration tests
- ✅ Full CI/CD pipeline with GitHub Actions

---

## Frontend Development (Angular 17)

### Role: Frontend Developer  
**Date:** 2024 - Present  
**Tech Stack:** Angular 17, TypeScript, PrimeNG, Tailwind CSS, Signals

### Scope
- Modern SPA with 12 feature modules
- State management using Angular Signals
- Responsive design with Tailwind CSS
- Integration with REST API

### Features
- Dashboard with KPI charts (Chart.js)
- Employee list with virtual scrolling
- GPS attendance check-in (Leaflet)
- Notification system with real-time updates
- Role-based access control UI
- Loading states and error handling

### Measurable Results
- ✅ Modern UI with PrimeNG components
- ✅ Lazy loading for optimal performance
- ✅ Type-safe development with TypeScript
- ✅ Consistent state management with Signals

---

## Backend Development (.NET 8)

### Role: Backend Developer  
**Date:** 2024 - Present  
**Tech Stack:** ASP.NET Core 8, CQRS, MediatR, MongoDB, Redis

### Scope
- RESTful API with Clean Architecture
- NoSQL database design (MongoDB)
- Background job processing (Hangfire)
- Authentication & authorization

### Features
- 19 Carter modules for endpoint routing
- CQRS pattern with MediatR
- FluentValidation for input validation
- Serilog structured logging
- Health checks (MongoDB, Redis)

### Measurable Results
- ✅ Well-structured layered architecture
- ✅ 23 MongoDB collections with proper indexing
- ✅ 5 automated background services
- ✅ Rate limiting and security middleware

---

## DevOps & Deployment

### Role: DevOps Engineer  
**Date:** 2024 - Present  
**Tools:** Docker, GitHub Actions, Render, Vercel

### Scope
- Containerized full-stack application
- CI/CD pipeline setup
- Cloud deployment management

### Features
- Docker Compose for local development
- Multi-stage Dockerfile builds
- GitHub Actions CI/CD workflows
- Production deployment to Render & Vercel

### Measurable Results
- ✅ Automated testing on every push
- ✅ Production-ready deployments
- ✅ MongoDB Atlas cluster configuration
- ✅ Redis caching strategy

---

## SKILLS & TECHNOLOGIES

### Backend
| Skill | Proficiency |
|-------|-------------|
| ASP.NET Core 8 | Advanced |
| Clean Architecture | Advanced |
| CQRS + MediatR | Advanced |
| MongoDB | Intermediate |
| Redis | Intermediate |
| JWT Authentication | Advanced |
| Hangfire | Intermediate |
| Docker | Intermediate |

### Frontend
| Skill | Proficiency |
|-------|-------------|
| Angular 17 | Advanced |
| TypeScript | Advanced |
| PrimeNG | Advanced |
| Tailwind CSS | Intermediate |
| RxJS | Intermediate |
| Signals | Advanced |

### DevOps
| Skill | Proficiency |
|-------|-------------|
| Docker | Intermediate |
| GitHub Actions | Intermediate |
| Vercel | Intermediate |
| Render | Intermediate |

---

## DOCUMENTATION

- Full API Reference: `docs/api-endpoint-reference.md`
- Architecture Overview: `docs/architecture-overview.md`
- Database Schema: `docs/database-schema.md`
- Module Flows: `docs/module-flow-diagrams.md`

---

## FEEDBACK & RECOGNITION

- Open-source project available for community use
- Production-ready codebase following industry best practices
- Clean Architecture implementation praised in code reviews
- Comprehensive documentation for easy onboarding

---

*Last Updated: March 2026*
