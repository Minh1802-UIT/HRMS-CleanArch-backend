# ADR-001: Naming Conventions

## Status
Accepted

## Context
The codebase had naming inconsistencies:
1. **Entity suffix**: 3 out of 19 entities used `Entity` suffix (`EmployeeEntity`, `ContractEntity`, `PayrollEntity`) while others did not.
2. **Enum suffix**: `LeaveTypeEnum` had a redundant `Enum` suffix.

## Decision

### Entity Suffix
The `Entity` suffix on `EmployeeEntity`, `ContractEntity`, and `PayrollEntity` is **intentional** and retained because:
- **`EmployeeEntity`**: The root namespace is `Employee.*`. Naming the class `Employee` would cause `Employee.Employee` ambiguity in every file that uses it.
- **`ContractEntity`** and **`PayrollEntity`**: Follow the same convention for consistency within the `HumanResource` and `Payroll` bounded contexts.

All other entities (`Department`, `Position`, `LeaveType`, `Shift`, etc.) do NOT have namespace conflicts, so they use plain names.

**Rule**: Only add the `Entity` suffix when the class name would conflict with a namespace segment.

### Enum Naming
- Renamed `LeaveTypeEnum` → `LeaveCategory` to:
  1. Remove the redundant `Enum` suffix (C# convention: enums should not have type suffixes).
  2. Avoid collision with the `LeaveType` entity class.

**Rule**: Enums should use descriptive names without type suffixes (`Enum`, `Type`, `Flag`).

## Consequences
- Developers know why certain entities have the suffix and should follow the pattern only when namespace collisions warrant it.
- New enums must never use `Enum` suffix.
