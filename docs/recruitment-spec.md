# Recruitment Feature Specification

## Overview

The Recruitment module manages the end-to-end hiring pipeline: job vacancies, candidates, interviews, AI-assisted CV parsing/scoring, and candidate onboarding to employee records.

---

## Data Model

### Entities

| Entity | Collection | Description |
|---|---|---|
| `JobVacancy` | `job_vacancies` | A job posting with title, description, requirements, status |
| `Candidate` | `candidates` | An applicant for a vacancy with stage, score, resume |
| `Interview` | `interviews` | A scheduled interview between a candidate and an employee (interviewer) |

### JobVacancy Fields

| Field | Type | Notes |
|---|---|---|
| `Id` | string | MongoDB ObjectId |
| `Title` | string | Required |
| `Description` | string | Required |
| `Vacancies` | int | Number of open positions (≥ 1) |
| `ExpiredDate` | DateTime | Application deadline |
| `Status` | `JobVacancyStatus` | Draft, Open, Closed |
| `Requirements` | List\<string\> | Line-separated requirements |
| `IsDeleted` | bool | Soft delete flag |
| `CreatedAt` | DateTime | UTC |

### Candidate Fields

| Field | Type | Notes |
|---|---|---|
| `Id` | string | MongoDB ObjectId |
| `FullName` | string | Required |
| `Email` | string | Required |
| `Phone` | string | Required |
| `JobVacancyId` | string | FK to JobVacancy |
| `Status` | `CandidateStatus` | Applied, Screening, Interviewing, Test, Hired, Rejected, Onboarded |
| `ResumeUrl` | string | Link to uploaded CV |
| `AppliedDate` | DateTime | When the candidate applied |
| `AiScore` | int? | 0-100 AI match score |
| `AiMatchingSummary` | string? | AI summary |
| `ExtractedSkills` | string? | JSON array of skills from CV |

### Interview Fields

| Field | Type | Notes |
|---|---|---|
| `Id` | string | MongoDB ObjectId |
| `CandidateId` | string | FK to Candidate |
| `InterviewerId` | string | FK to Employee |
| `ScheduledTime` | DateTime | Interview start |
| `DurationMinutes` | int | Default 60 |
| `Location` | string | Default "Online" |
| `Status` | `InterviewStatus` | Scheduled, Completed, Cancelled |
| `Feedback` | string | Interviewer feedback |

### Status Transition Rules (Candidate)

```
Applied → Interviewing | Rejected
Interviewing → Test | Hired | Rejected
Test → Hired | Rejected
Hired → Onboarded | Rejected
Onboarded (terminal)
Rejected (terminal)
```

### Status Transition Rules (JobVacancy)

```
Draft ↔ Open
Open → Closed
Closed → Draft
```

---

## API Endpoints

### Job Vacancies

| Method | Path | Description |
|---|---|---|
| GET | `/api/recruitment/vacancies` | List all vacancies (or paginated if `pageSize` query param is set) |
| GET | `/api/recruitment/vacancies/{id}` | Get vacancy by ID |
| POST | `/api/recruitment/vacancies` | Create vacancy |
| PATCH | `/api/recruitment/vacancies/{id}` | Update vacancy |
| POST | `/api/recruitment/vacancies/{id}/close` | Close vacancy |
| DELETE | `/api/recruitment/vacancies/{id}` | Soft-delete vacancy |
| GET | `/api/recruitment/vacancies/options` | Get filter options (offices, employment types) |

### Candidates

| Method | Path | Description |
|---|---|---|
| GET | `/api/recruitment/candidates` | List candidates (or paginated if `pageSize` query param is set) |
| GET | `/api/recruitment/candidates/{id}` | Get candidate by ID |
| POST | `/api/recruitment/candidates` | Create candidate |
| PATCH | `/api/recruitment/candidates/{id}` | Update candidate |
| POST | `/api/recruitment/candidates/{id}/status` | Update candidate status |
| POST | `/api/recruitment/candidates/{id}/onboard` | Onboard candidate as employee |
| DELETE | `/api/recruitment/candidates/{id}` | Soft-delete candidate |
| POST | `/api/recruitment/candidates/parse-cv` | Parse CV PDF with AI |
| POST | `/api/recruitment/candidates/{id}/score` | Score candidate with AI |

### Interviews

| Method | Path | Description |
|---|---|---|
| GET | `/api/recruitment/interviews` | List interviews |
| GET | `/api/recruitment/interviews/{id}` | Get interview by ID |
| POST | `/api/recruitment/interviews` | Create interview |
| PATCH | `/api/recruitment/interviews/{id}` | Update interview |
| POST | `/api/recruitment/interviews/{id}/review` | Submit interview review |
| DELETE | `/api/recruitment/interviews/{id}` | Delete interview |

### Pagination

Both `GET /api/recruitment/vacancies` and `GET /api/recruitment/candidates` support pagination via query parameters:

| Param | Type | Default |
|---|---|---|
| `pageNumber` | int? | 1 |
| `pageSize` | int? | 20 (max 500) |
| `searchTerm` | string? | — |
| `sortBy` | string? | CreatedAt (vacancies), AppliedDate (candidates) |
| `isDescending` | bool? | false |

When `pageSize` is provided, returns `PagedResult<T>` with `{ items, totalCount, pageNumber, pageSize }`.

---

## Architecture

### Backend

- **Pattern:** CQRS + MediatR
- **Controllers:** Carter module endpoints (`/api/recruitment/`)
- **Auth:** `RequireRole("Admin", "HR")` on all endpoints
- **Database:** MongoDB with soft delete

### Frontend

- **Framework:** Angular 17+ standalone components
- **Routing:** Lazy-loaded at `/recruitment` and `/recruitment/candidates/:id`
- **UI:** PrimeNG + Tailwind CSS, glass-morphism panels
- **State:** RxJS Observables + Signals (partial)
- **Views:**
  - **Jobs tab:** Table with list view + kanban board
  - **Candidates tab:** Searchable/filterable table
  - **Process tab:** Visual stage flow diagram

---

## Features

### AI Integration

- **CV Parsing:** Accepts PDF upload, extracts name, email, phone, skills via OpenAI GPT
- **Candidate Scoring:** Generates match score (0-100) and summary based on vacancy requirements

### Onboarding

- Converts a Hired candidate to a full Employee record
- Required: employee code, department, position, manager, join date, date of birth

### Dashboard

The HR dashboard displays:
- Active Jobs count
- Interviews Today count
- Recruitment funnel (candidates by stage)
- RecruitmentStats: JobOpenings, NewCandidates, Interviewed, PendingFeedback

---

## MongoDB Indexes

| Collection | Index | Purpose |
|---|---|---|
| `candidates` | `jobVacancyId` | Filter by vacancy |
| `candidates` | `status` | Dashboard stats |
| `candidates` | `IsDeleted + AppliedDate` DESC | Paged list sort |
| `candidates` | Text: `FullName` | Search term |
| `interviews` | `ScheduledTime` | "Interviews Today" |
| `interviews` | `CandidateId` | Get by candidate |
| `interviews` | `ScheduledTime` DESC | Paged list sort |
| `job_vacancies` | `IsDeleted` | Base filter |
| `job_vacancies` | `IsDeleted + Status` | Active vacancy count |
| `job_vacancies` | `IsDeleted + CreatedAt` DESC | Paged list sort |
| `job_vacancies` | Text: `Title` | Search term |

---

## Open Issues / TODOs

- [ ] **Candidate extended fields:** `experience`, `education`, `notes` arrays not yet returned by backend or displayed in candidate detail
- [ ] **Onboard validation:** Employee code uniqueness not checked on frontend before submit
- [ ] **Kanban board filters:** Currently filters by `selectedJobDetail.title` string match; should use `jobVacancyId`
- [ ] **Pagination params:** Frontend currently uses client-side pagination; should switch to server-side `getVacanciesPaged`/`getCandidatesPaged` for large datasets
- [ ] **Interviews Today hardcoded:** The "Dec 9" placeholder date in kanban header should display actual vacancy expiry date
- [ ] **Department/Office filter:** Jobs tab department filter options are hardcoded in HTML; should come from API via recruitment options endpoint
