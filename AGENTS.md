# HRMS â€“ Agent Instructions

Du an HRMS gom **backend** (.NET 8 Clean Architecture) va **frontend** (Angular 17).

## Repo structure (workspace)

- **Backend:** `HRMS-CleanArch-backend/` â€“ API, Domain, Application, Infrastructure.
- **Frontend:** `HRMS-CleanArch-frontend/` â€“ Angular SPA (PrimeNG, Tailwind).
- **Specs & SDLC:** Trong backend: `sdlc/specs/` â€“ requirements theo REQ-XXX, moi REQ co `SPEC.md` va `decisions/`.

## Khi lam viec voi agent

1. **Doc spec truoc:** Neu task thuoc requirement cu the, doc `sdlc/specs/REQ-XXX/SPEC.md` va `decisions/` (neu co).
2. **Backend:** Tuan thu Clean Architecture (Domain <- Application <- Infrastructure, API goi Application). Dung MediatR CQRS, FluentValidation.
3. **Frontend:** Standalone components, lazy routes. State: uu tien Signals, tranh trung lap voi BehaviorSubject.
4. **API:** camelCase JSON, versioning qua URL/header. Response boc trong `ApiResponse<T>`.

## Cau truc thu muc (trong HRMS-CleanArch-backend)

### .cursor/ (Cursor AI Configuration)
- `.cursor/agents/` â€“ Custom subagents (hrms-architect, hrms-code-writer, hrms-tester)
- `.cursor/rules/` â€“ Coding standards (mdc files)
- `.cursor/skills/` â€“ Custom skills (SKILL.md files)
- `.cursor/plans/` â€“ Execution plans
- `.cursor/agent-memory/` â€“ Agent context

### sdlc/ (Software Development Life Cycle)
- `sdlc/context/` â€“ Architecture overview, glossary
- `sdlc/specs/REQ-XXX/` â€“ Feature specifications
- `sdlc/decisions/` â€“ Architectural Decision Records (ADRs)
- `sdlc/plans/` â€“ Feature implementation plans
- `sdlc/templates/` â€“ Project templates

Khi tao feature moi, them `sdlc/specs/REQ-XXX/SPEC.md` va cap nhat checklist trong do.
