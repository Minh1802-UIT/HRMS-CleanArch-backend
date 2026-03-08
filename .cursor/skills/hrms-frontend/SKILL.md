---
name: hrms-frontend
description: Frontend development cho HRMS. Tao Angular component, page, service voi Signals, PrimeNG, Tailwind. Su dung khi user yeu cau lam UI, them trang, hoac frontend feature.
---

# HRMS Frontend Development

## Tech Stack
- Angular 17+
- Standalone Components
- Signals cho state management
- PrimeNG UI components
- Tailwind CSS
- Lazy loading routes

## Workflow

### 1. Tao Component/Page
- Dung Angular CLI: ng g c
- Standalone: them standalone: true
- Dat trong features/ hoac components/

### 2. State Management
- Uu tien Signal: signal(), computed(), effect()
- Tranh dung BehaviorSubject neu khong can thiet
- State phuc tap: tao store folder rieng

### 3. API Integration
- Tao service goi API (HttpClient)
- Xu ly loading, error states
- Dung async pipe hoac toSignal()

### 4. UI Components
- PrimeNG: dung da co neu phu hop
- Tailwind: custom styles trong component
- Responsive design

## Tham chieu
- AGENTS.md - huong dan chung
- Frontend co the tham khao sdlc/specs/REQ-XXX/SPEC.md
