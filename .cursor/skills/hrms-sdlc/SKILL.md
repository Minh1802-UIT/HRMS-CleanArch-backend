---
name: hrms-sdlc
description: Viet SPEC.md va requirements cho HRMS. Tao file spec moi trong sdlc/specs/REQ-XXX/, theo template co san. Su dung khi user yeu cau viet spec, requirement, hoac tao feature moi.
---

# HRMS SDLC - Spec and Requirements

## Workflow

### 1. Kiem tra requirement da ton tai

- Liet ke cac file trong sdlc/specs/ de xem da co REQ-XXX nao chua.
- Neu chua co, tao moi voi so REQ tiep theo.

### 2. Tao SPEC.md theo template

- Template: sdlc/specs/REQ-001-sample-feature/SPEC.md
- Cac truong can dien:
  - Summary: Mo ta ngan gon feature
  - Background: Ly do, context, module lien quan
  - Requirements:
    - Functional (FR-1, FR-2, ...)
    - Non-functional (NFR-1, NFR-2, ...)
  - Acceptance Criteria: Tieu chi de accept
  - Technical Notes: Backend/Frontend lien quan, API contract
  - References: Link docs, tickets

### 3. Tao thu muc decisions (neu can)

- sdlc/specs/REQ-XXX/decisions/ chua ADR neu co architectural decision.

### 4. Cap nhap context

- Neu co thay doi ve glossary hoac kien truc chung, cap nhap sdlc/context/.

## Tham chieu

- sdlc/specs/REQ-001-sample-feature/SPEC.md - template spec
- sdlc/specs/REQ-001-sample-feature/decisions/.gitkeep - noi luu ADR
- sdlc/context/.gitkeep - context/glossary
