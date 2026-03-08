---
name: hrms-test
description: Viet unit tests va integration tests cho HRMS. Backend: xUnit, FluentAssertions, Moq. Frontend: Jest hoac Angular testing utilities. Su dung khi user yeu cau viet tests.
---

# HRMS Testing

## Tech Stack

### Backend
- xUnit
- FluentAssertions
- Moq
- InMemoryDatabase cho integration tests

### Frontend
- Jest
- Angular TestBed
- Testing Library

## Workflow

### 1. Unit Tests (Backend)
- Test Handler: mock Repository, goi Handle(), assert result
- Test Validator: mock dependency, assert ValidationResult
- Naming: Method_Scenario_ExpectedResult
- Coverage: it nhat business logic chinh

### 2. Integration Tests
- Test API endpoint: client.PostAsync(), assert response
- Seed data: InMemoryDatabase hoac test container
- Cleanup sau moi test

### 3. Frontend Tests
- Component: render(), fixture.detectChanges()
- Service: mock HttpTestingModule
- Integration: test navigation, form submission

## Best Practices
- Test name mo ta ro hanh vi
- Arrange-Act-Assert ro rang
- Tranh test nhieu thu trong 1 test
- Mock external dependencies
