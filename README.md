<div align="center">

# HRMS

### Human Resource Management System

ASP.NET Core 8 REST API for the HRMS platform — Clean Architecture, CQRS, MongoDB, and Hangfire background jobs.

[![CI - Backend](https://img.shields.io/github/actions/workflow/status/Minh1802-UIT/EmployeeCleanArch/ci.yml?branch=main&label=CI&logo=githubactions&logoColor=white)](https://github.com/Minh1802-UIT/EmployeeCleanArch/actions/workflows/ci.yml)
[![CD - Backend](https://img.shields.io/github/actions/workflow/status/Minh1802-UIT/EmployeeCleanArch/cd.yml?branch=main&label=CD&logo=githubactions&logoColor=white)](https://github.com/Minh1802-UIT/EmployeeCleanArch/actions/workflows/cd.yml)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![MongoDB](https://img.shields.io/badge/MongoDB-7.0-47A248?logo=mongodb)
![Redis](https://img.shields.io/badge/Redis-alpine-DC382D?logo=redis)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker)
![License](https://img.shields.io/badge/license-MIT-blue)

[🚀 Live Demo](https://hrms-clean-arch-frontend.vercel.app) &nbsp;|&nbsp;
[📖 API Docs](https://hrms-api.onrender.com/swagger) &nbsp;|&nbsp;
[🎨 Frontend Repo](https://github.com/Minh1802-UIT/HRMS-UI) &nbsp;|&nbsp;
[🏗️ Architecture](docs/architecture-overview.md) &nbsp;|&nbsp;
[📚 Full Docs](docs/)

</div>

---

## Table of Contents

- [✨ Features](#-features)
- [🏗️ Architecture](#️-architecture)
- [🛠️ Tech Stack](#️-tech-stack)
- [🚀 Getting Started](#-getting-started)
- [⚙️ Environment Variables](#️-environment-variables)
- [🐳 Docker](#-docker)
- [🧪 Testing](#-testing)
- [📖 Documentation](#-documentation)
- [📁 Project Structure](#-project-structure)
- [🚢 Deployment](#-deployment)
- [🐍 Scripts](#-scripts)
- [⚙️ Background Services](#️-background-services)
- [🔒 Security Notes](#-security-notes)

---

> **Demo credentials** — Username: `admin@hrms.com` &nbsp;·&nbsp; Password: set via `SEEDING_DEFAULT_PASSWORD` in `.env`

---

## ✨ Features

| Module | Capabilities |
|---|---|
| **Authentication** | JWT + Refresh Token Rotation, Forgot/Reset Password, Role-based access, Force-change password on first login |
| **Employee Management** | Create/Update/Delete employees, Org Chart, Lookup, Soft delete, Optimistic concurrency |
| **Contract Management** | Fixed-term & indefinite contracts, Salary components (base + allowances), Contract expiry notifications |
| **Departments & Positions** | Hierarchical tree structure (`parentId`), Salary range per position |
| **Attendance** | Real-time check-in/check-out, Raw log pipeline → Attendance bucket, Overtime calculation, Logical-day handling for overnight shifts |
| **Shift Management** | Multiple shifts, Grace period, Overnight shift support |
| **Leave Management** | Leave types with Sandwich Rule & Accrual, Balance tracking, Leave allocation per year, Carry-forward, Manager approval workflow |
| **Payroll** | Monthly payroll generation, Vietnamese PIT (7-tier progressive tax), BHXH/BHYT/BHTN deductions, Debt carry-over, PDF payslip export, Excel bulk export |
| **Payroll Cycles** | Standardized working day snapshots per month, Public holiday exclusion |
| **Recruitment** | Job vacancies, Multi-stage candidate pipeline (Applied → Screening → Interview → Offered → Hired), One-click onboarding → Employee |
| **Performance** | Goal tracking with progress updates, Period reviews with scoring |
| **Notifications** | In-app notification feed, Unread count badge, Read/Read-all |
| **Dashboard** | Aggregated KPIs: headcount, leave, recruitment stats |
| **Audit Logs** | Full mutation history with offset and cursor pagination |
| **File Upload** | Avatar, CV, contract PDFs — backed by Supabase Storage in prod |

---

## 🏗️ Architecture

```
┌──────────────────────────────┐     HTTPS      ┌──────────────────────┐
│  Angular 17 (Vercel / nginx) │ ◄────────────► │ ASP.NET Core 8 API   │
│  PrimeNG · Tailwind CSS      │                │ (Render / Docker)    │
└──────────────────────────────┘                └──────────┬───────────┘
                                                           │
              ┌────────────────────────────────────────────┤
              │                          │                 │
   ┌──────────┴───────┐     ┌────────────┴────────┐  ┌────┴──────────────┐
   │ MongoDB Atlas     │     │ Redis (Hangfire +   │  │ Supabase Storage  │
   │ (primary DB)      │     │  rate-limit cache)  │  │ (file uploads)    │
   └───────────────────┘     └─────────────────────┘  └───────────────────┘
```

**Backend layers (Clean Architecture):**

```
Employee.API            ← Carter endpoints, Middleware, DI composition root
Employee.Application    ← CQRS handlers (MediatR), Validators, Services, DTOs
Employee.Domain         ← Entities, Value Objects, Domain Events, Interfaces
Employee.Infrastructure ← MongoDB repos, Identity, Hangfire, Email, File storage
```

---

## 🛠️ Tech Stack

### Backend

| Technology | Version | Purpose |
|---|---|---|
| ASP.NET Core | 8.0 | Web API framework |
| Carter | 8.2.1 | Minimal API module routing |
| MediatR | 12.4.1 | CQRS pipeline (commands + queries) |
| FluentValidation | 11.9.0 | Input validation |
| MongoDB.Driver | 3.6.0 | Primary database |
| AspNetCore.Identity.MongoDbCore | 7.0.0 | User/Role management |
| Hangfire | 1.8.17 | Background job queue (backed by Redis) |
| StackExchange.Redis | — | Caching + rate limiting |
| QuestPDF | 2026.2.1 | PDF payslip generation |
| ClosedXML | 0.105.0 | Excel payroll export |
| SendGrid | 9.29.3 | Transactional email |
| Serilog | 10.0.0 | Structured logging |
| JWT Bearer | 8.0.0 | Authentication |

### Infrastructure

| Service | Purpose |
|---|---|
| MongoDB Atlas | Production database |
| Redis (local) / Upstash (cloud) | Session cache, Hangfire storage, rate limiting |
| Supabase Storage | File uploads (avatars, contracts, CVs) |
| Docker + docker-compose | Containerized local development |
| Render | Backend hosting |
| Vercel | Frontend hosting — see [HRMS-UI](https://github.com/Minh1802-UIT/HRMS-UI) |

---

## 📁 Project Structure

```
EmployeeCleanArch/
├── Employee.API/                   # Presentation (Carter modules, middleware)
│   ├── Endpoints/                  # 19 Carter modules (one per domain area)
│   ├── Middlewares/                # GlobalExceptionHandler, CorrelationId
│   ├── Common/                     # ApiResponse wrapper, helpers
│   └── Program.cs                  # App composition root
│
├── Employee.Application/           # Application (CQRS, business logic)
│   ├── Features/
│   │   ├── Auth/                   # Commands + Queries for identity
│   │   ├── HumanResource/          # Employee CRUD + event handlers
│   │   ├── Leave/                  # Leave requests, allocations, types
│   │   ├── Payroll/                # Payroll generation, cycles, PDF
│   │   ├── Attendance/             # Check-in/out, processing service
│   │   ├── Recruitment/            # Vacancies, candidates, interviews, onboarding
│   │   ├── Organization/           # Departments, positions, shifts
│   │   ├── Performance/            # Goals, reviews
│   │   └── Notifications/          # In-app notification service
│   └── Common/                     # Interfaces, exceptions, base DTOs
│
├── Employee.Domain/                # Domain (entities, events, value objects)
│   ├── Entities/                   # Core business entities
│   ├── Events/                     # Domain events
│   ├── ValueObjects/               # SalaryComponents, SalaryRange, etc.
│   └── Services/Payroll/           # ITaxCalculator / VietnameseTaxCalculator
│
├── Employee.Infrastructure/        # Infrastructure (DB, external services)
│   ├── Persistence/                # MongoDB context, indexes
│   ├── Repositories/               # All IRepository implementations
│   ├── Identity/                   # IdentityService, TokenService
│   ├── BackgroundServices/         # 5 hosted services (Attendance, Payroll, etc.)
│   └── Services/                   # Email, File storage, Payslip PDF, Excel
│
├── Employee.UnitTests/             # xUnit + Moq unit tests
├── Employee.IntegrationTests/      # xUnit + WebApplicationFactory
├── scripts/                        # Python seed scripts
├── docs/                           # Architecture, DB schema, API reference docs
├── docker-compose.yml              # Local development stack
├── docker-compose.prod.yml         # Production overlay
├── .env.example                    # Environment variable template
└── Dockerfile                      # Multi-stage build for API
```

---

## 🚀 Getting Started

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| Docker Desktop | Latest |
| MongoDB | 7.x (via Docker) |
| Redis | Alpine (via Docker) |

### 1. Clone the repository

```bash
git clone https://github.com/<your-username>/EmployeeCleanArch.git
cd EmployeeCleanArch
```

### 2. Set up environment variables

```bash
cp .env.example .env
```

Edit `.env` and fill in all `CHANGE_ME_*` values (see [Environment Variables](#environment-variables)).

### 3. Configure API secrets

For local development without Docker, use .NET User Secrets:

```bash
cd Employee.API
dotnet user-secrets set "JwtSettings:Key" "your-min-32-char-secret-key-here!!"
dotnet user-secrets set "EmailSettings:Password" "your-smtp-app-password"
dotnet user-secrets set "Seeding:DefaultPassword" "Admin@123456"
```

### 4. Start infrastructure services

```bash
# Start only MongoDB + Redis (without building the .NET API in Docker)
docker compose up app_db app_cache -d
```

### 5. Run the API

```bash
cd Employee.API
dotnet run
```

API is available at: `http://localhost:5055`  
Swagger UI: `http://localhost:5055/swagger`  
Hangfire Dashboard: `http://localhost:5055/hangfire` _(Admin only)_

> To run the Angular frontend alongside, see the [HRMS-UI repository](https://github.com/Minh1802-UIT/HRMS-UI).

---

## ⚙️ Environment Variables

All required variables are listed in [`.env.example`](.env.example).  
Copy to `.env` — this file is **gitignored** and must never be committed.

| Variable | Required | Description |
|---|---|---|
| `MONGO_USER` | ✅ | MongoDB admin username |
| `MONGO_PASSWORD` | ✅ | MongoDB admin password |
| `JWT_SECRET_KEY` | ✅ | JWT signing key (min 32 characters) |
| `REDIS_PASSWORD` | ✅ | Redis authentication password |
| `EMAIL_SMTP_HOST` | ✅ | SMTP server (e.g. `smtp.gmail.com`) |
| `EMAIL_SMTP_PORT` | ✅ | SMTP port (typically `587`) |
| `EMAIL_SENDER` | ✅ | Sender email address |
| `EMAIL_PASSWORD` | ✅ | SMTP password or app password |
| `CORS_ALLOWED_ORIGINS` | ✅ | Frontend origin (e.g. `https://hrms.example.com`) |
| `SEEDING_DEFAULT_PASSWORD` | ✅ | Default password for seeded accounts |
| `DOCKER_USERNAME` | Docker only | Docker Hub username for prod image pull |

**appsettings.json keys** (directly in config for non-secret values):

| Key | Default | Description |
|---|---|---|
| `JwtSettings:DurationInMinutes` | `60` (dev) / `30` (prod) | Access token TTL |
| `SystemSettings:TimezoneId` | `Asia/Ho_Chi_Minh` | Timezone for all date logic |
| `BackgroundJobs:AttendanceProcessingIntervalMinutes` | `5` | Attendance sweep interval |
| `BackgroundJobs:LeaveAccrualIntervalHours` | `6` | Leave accrual check interval |
| `BackgroundJobs:PayrollIntervalHours` | `12` | Auto-payroll check interval |
| `SupabaseStorage:ProjectUrl` | — | Supabase project URL |
| `SupabaseStorage:ServiceKey` | — | Supabase service role key |
| `SupabaseStorage:BucketName` | `employee-files` | Storage bucket name |

---

## 🐳 Docker

### Development (full stack)

```bash
# Copy and fill in .env
cp .env.example .env

# Build and start all services (MongoDB, Redis, .NET API, Angular/nginx)
docker compose up --build

# Access
# API:         http://localhost:5000
# Frontend:    http://localhost:80
# Hangfire:    http://localhost:5000/hangfire
```

### Production

```bash
# Uses docker-compose.prod.yml as an override (Production env, resource limits)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Service overview

| Container | Image | Exposed Port | Notes |
|---|---|---|---|
| `employee_mongo_db` | `mongo:7` | Internal only | Healthcheck enabled |
| `employee_redis_cache` | `redis:alpine` | Internal only | Password-protected |
| `employee_api` | Built from `Dockerfile` | `5000:8080` | Depends on MongoDB healthy |
| `employee_nginx` | Built from `HRMS-UI/Dockerfile` | `80:80` | Angular + nginx |

---

## 🧪 Testing

### Unit Tests

```bash
cd Employee.UnitTests
dotnet test
```

Coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Tests

Integration tests use `WebApplicationFactory` — they spin up the full API in-memory and connect to a test MongoDB instance.

```bash
cd Employee.IntegrationTests
dotnet test
```

> **Note:** Integration tests require a running MongoDB. Configure the connection string in `appsettings.Development.json` or via environment variables before running.

### All tests

```bash
# From solution root
dotnet test EmployeeCleanArch.sln
```

---

## 📖 Documentation

Full API reference: [`docs/api-endpoint-reference.md`](docs/api-endpoint-reference.md)

Key conventions:
- **Base URL:** `/api/`
- **Auth:** `Authorization: Bearer <accessToken>`
- **Refresh token:** `httpOnly` cookie `refreshToken` (auto-managed by browser)
- **Response envelope:**
  ```json
  {
    "succeeded": true,
    "message": "...",
    "data": {},
    "errors": null,
    "errorCode": null
  }
  ```

**Available documentation:**

| File | Contents |
|---|---|
| [`docs/api-endpoint-reference.md`](docs/api-endpoint-reference.md) | Full endpoint reference with request/response schemas |
| [`docs/architecture-overview.md`](docs/architecture-overview.md) | Layer diagram, design patterns, dependency flow |
| [`docs/database-schema.md`](docs/database-schema.md) | All 23 MongoDB collections + Mermaid ER diagram |
| [`docs/module-flow-diagrams.md`](docs/module-flow-diagrams.md) | Sequence diagrams, state machines, flow charts per module |

Interactive Swagger UI available at `/swagger` when running the API.

---

## 🚢 Deployment

### Backend — Render

The backend is deployed on [Render](https://render.com) as a Web Service using the `Dockerfile` in the repo root.

**Required environment variables** on Render (Settings → Environment):

```
ASPNETCORE_ENVIRONMENT=Production
EmployeeDatabaseSettings__ConnectionString=mongodb+srv://<user>:<pass>@cluster.mongodb.net/
EmployeeDatabaseSettings__DatabaseName=EmployeeCleanDB
JwtSettings__Key=<min-32-char-secret>
RedisSettings__ConnectionString=<upstash-or-redis-url>
EmailSettings__SenderEmail=<sender>
EmailSettings__Password=<smtp-password>
SupabaseStorage__ProjectUrl=<supabase-url>
SupabaseStorage__ServiceKey=<supabase-service-key>
CorsSettings__AllowedOrigins__0=https://your-frontend.vercel.app
FrontendUrl=https://your-frontend.vercel.app
```

---

## 🐍 Scripts

Python seed scripts for initial data setup (requires `pymongo`):

```bash
pip install pymongo
```

| Script | Purpose |
|---|---|
| `scripts/seed_public_holidays_2026.py` | Seeds Vietnamese public holidays for 2026 |
| `scripts/seed_payroll_cycles.py` | Seeds payroll cycles for the year |

```bash
# Run with connection string
python scripts/seed_public_holidays_2026.py --uri "mongodb://localhost:27017" --db "EmployeeCleanDB"
```

---

## ⚙️ Background Services

Five hosted services run automatically on startup:

| Service | Interval | Trigger condition |
|---|---|---|
| `AttendanceProcessingBackgroundJob` | Every 5 min | Always — sweeps unprocessed punch logs |
| `LeaveAccrualBackgroundService` | Every 6 hours | First day of month — accrues monthly leave |
| `PayrollBackgroundService` | Every 12 hours | Day ≥ 28 or Day = 1 — auto-calculates payroll |
| `ContractExpirationBackgroundService` | Every 24 hours | Sends notification if contract expires within 30 days |
| `SoftDeleteCleanupBackgroundService` | Configurable | Permanently removes soft-deleted records past retention period |

All services implement retry logic (3 attempts with exponential back-off).

---

## 🔒 Security Notes

- **JWT keys** and **database credentials** must be set via environment variables or .NET User Secrets — never committed to source control.
- Refresh tokens use **token rotation** with **reuse detection** (entire token family is revoked on replay attack).
- **Single-session policy**: logging in revokes all previous refresh tokens.
- **Rate limiting** applied on login, refresh, check-in, write operations, and file uploads.
- **MongoDB port** (27017) and **Redis port** (6379) are not exposed externally in Docker — only reachable within the Docker bridge network.
- The `logs/` directory is excluded from Docker images; logs are written to mounted volumes or stdout.

---

## 📄 License

This project is released under the **MIT License** — free for educational and portfolio use.

---

<div align="center">

Made with ❤️ using .NET 8 &nbsp;·&nbsp; [Frontend →](https://github.com/Minh1802-UIT/HRMS-UI)

</div>
