---
name: hrms-backend
description: Backend development cho HRMS. Tao CQRS command/query voi MediatR, FluentValidation, Clean Architecture layers. Su dung khi user yeu cau lam backend API, them endpoint, hoac backend feature.
---

# HRMS Backend Development

## Tech Stack
- .NET 8
- Clean Architecture (Domain, Application, Infrastructure, API)
- MediatR (CQRS pattern)
- FluentValidation
- Entity Framework Core

## Workflow

### 1. Tao Domain Entity (neu can)
- Them trong Domain/Entities/
- Implement interfaces trong Application/Interfaces/

### 2. Tao CQRS Command hoac Query
- Command: Application/Commands/
- Query: Application/Queries/
- Dinh kem Handler va Validator

### 3. Tao Endpoint (API Controller)
- Them trong API/Controllers/
- Dung [ApiController] attribute
- Goi MediatR.Send()
- Response wrapped trong ApiResponse<T>

### 4. Validation
- Tao FluentValidation validator cho command/query
- Dat trong cung folder voi command/query

## Tham chieu
- AGENTS.md - huong dan chung
- Clean Architecture diagram trong sdlc/context/
