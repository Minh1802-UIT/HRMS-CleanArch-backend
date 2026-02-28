using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Leave;
using Employee.Application.Features.Leave.Dtos;
using Employee.Application.Features.Leave.Mappers;
using Employee.Application.Common.Models;
using Employee.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Employee.Application.Features.Leave.Services
{
    public class LeaveAllocationService : ILeaveAllocationService
    {
        private readonly ILeaveAllocationRepository _allocationRepo;
        private readonly ILeaveTypeRepository _leaveTypeRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public LeaveAllocationService(
            ILeaveAllocationRepository allocationRepo,
            ILeaveTypeRepository leaveTypeRepo,
            IEmployeeRepository employeeRepo)
        {
            _allocationRepo = allocationRepo;
            _leaveTypeRepo = leaveTypeRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<IEnumerable<LeaveAllocationDto>> GetBalanceByEmployeeIdAsync(string employeeId)
        {
            var allocations = await _allocationRepo.GetByEmployeeIdAsync(employeeId);
            var leaveTypesPaged = await _leaveTypeRepo.GetPagedAsync(new PaginationParams { PageSize = 100 });
            var leaveTypes = leaveTypesPaged.Items;
            var typeMap = leaveTypes.ToDictionary(t => t.Id, t => t.Name);

            var employees = await _employeeRepo.GetLookupAsync(employeeId, 1);
            var empName = employees.Any() ? employees.First().Label : "Unknown";
            var empCode = (employees.Any() ? employees.First().SecondaryLabel : null) ?? "Unknown";

            return allocations.Select(a =>
            {
                var typeName = typeMap.GetValueOrDefault(a.LeaveTypeId) ?? "Unknown";
                return a.ToDto(typeName, empName, empCode);
            });
        }

        public async Task<PagedResult<LeaveAllocationDto>> GetAllAllocationsAsync(PaginationParams pagination, string? keyword = null)
        {
            List<string>? employeeIds = null;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var employees = await _employeeRepo.GetLookupAsync(keyword, 100);
                if (!employees.Any())
                {
                    return new PagedResult<LeaveAllocationDto>
                    {
                        Items = new List<LeaveAllocationDto>(),
                        TotalCount = 0,
                        PageNumber = pagination.PageNumber ?? 1,
                        PageSize = pagination.PageSize ?? 10
                    };
                }
                employeeIds = employees.Select(x => x.Id).ToList();
            }

            var pagedAllocations = await _allocationRepo.GetPagedAsync(pagination, employeeIds);
            var leaveTypesPaged = await _leaveTypeRepo.GetPagedAsync(new PaginationParams { PageSize = 100 });
            var typeMap = leaveTypesPaged.Items.ToDictionary(t => t.Id, t => t.Name);

            var uniqueEmployeeIds = pagedAllocations.Items.Select(x => x.EmployeeId).Distinct().ToList();
            var empNames = await _employeeRepo.GetNamesByIdsAsync(uniqueEmployeeIds);

            var dtos = pagedAllocations.Items.Select(a =>
            {
                var typeName = typeMap.GetValueOrDefault(a.LeaveTypeId) ?? "Unknown";
                var empInfo = empNames.GetValueOrDefault(a.EmployeeId);
                var empName = !string.IsNullOrEmpty(empInfo.Name) ? empInfo.Name : "Unknown";
                var empCode = empInfo.Code ?? "";

                return a.ToDto(typeName, empName, empCode);
            }).ToList();

            return new PagedResult<LeaveAllocationDto>
            {
                Items = dtos,
                TotalCount = pagedAllocations.TotalCount,
                PageNumber = pagination.PageNumber ?? 1,
                PageSize = pagination.PageSize ?? 10
            };
        }

        public async Task<LeaveAllocationDto?> GetByEmployeeAndTypeAsync(string employeeId, string leaveTypeId, string year)
        {
            var allocation = await _allocationRepo.GetByEmployeeAndTypeAsync(employeeId, leaveTypeId, year);
            if (allocation == null) return null;

            var type = await _leaveTypeRepo.GetByIdAsync(leaveTypeId);
            var typeName = type?.Name ?? "Unknown";

            var empNames = await _employeeRepo.GetNamesByIdsAsync(new List<string> { employeeId });
            var empInfo = empNames.GetValueOrDefault(employeeId);
            var empName = !string.IsNullOrEmpty(empInfo.Name) ? empInfo.Name : "Unknown";

            return allocation.ToDto(typeName, empName);
        }

        public async Task AllocateDaysAsync(CreateAllocationDto dto)
        {
            var allocation = dto.ToEntity();
            await _allocationRepo.CreateAsync(allocation);
        }

        public async Task DeleteAsync(string id) => await _allocationRepo.DeleteAsync(id);

        public async Task InitializeAllocationAsync(string employeeId, string year)
        {
            var leaveTypesPaged = await _leaveTypeRepo.GetPagedAsync(new PaginationParams { PageSize = 100 });
            var leaveTypes = leaveTypesPaged.Items;

            // 1. Bulk Fetch existing allocations for the employee
            var existingAllocations = (await _allocationRepo.GetByEmployeeIdsAndYearAsync(new List<string> { employeeId }, year))
                .ToDictionary(a => a.LeaveTypeId);

            var allocationsToUpsert = new List<LeaveAllocation>();

            foreach (var type in leaveTypes)
            {
                if (!existingAllocations.TryGetValue(type.Id, out var existing))
                {
                    var days = type.IsAccrual ? 0 : type.DefaultDaysPerYear;
                    var allocation = new LeaveAllocation(employeeId, type.Id, year, days);
                    allocation.SetId(Guid.NewGuid().ToString());

                    if (type.IsAccrual)
                    {
                        allocation.UpdateAccrual(type.AccrualRatePerMonth, DateTime.UtcNow.ToString("yyyy-MM"));
                    }
                    allocationsToUpsert.Add(allocation);
                }
                else if (existing.NumberOfDays == 0 && type.DefaultDaysPerYear > 0)
                {
                    existing.UpdateAllocation(type.DefaultDaysPerYear);
                    allocationsToUpsert.Add(existing);
                }
            }

            // 2. Bulk Upsert
            if (allocationsToUpsert.Any())
            {
                await _allocationRepo.BulkUpsertAsync(allocationsToUpsert);
            }
        }

        public async Task RunMonthlyAccrualAsync()
        {
            var now = DateTime.UtcNow;
            var year = now.Year.ToString();
            var currentMonthKey = now.ToString("yyyy-MM");

            var activeEmployees = (await _employeeRepo.GetAllAsync())
                .Where(e => e.JobDetails != null && (e.JobDetails.Status == EmployeeStatus.Active || e.JobDetails.Status == EmployeeStatus.Probation))
                .ToList();

            var activeEmployeeIds = activeEmployees.Select(e => e.Id).ToList();

            var leaveTypesPaged = await _leaveTypeRepo.GetPagedAsync(new PaginationParams { PageSize = 100 });
            var accrualTypes = leaveTypesPaged.Items.Where(x => x.IsAccrual).ToList();

            // 1. Bulk Fetch existing allocations for all active employees
            var allAllocations = (await _allocationRepo.GetByEmployeeIdsAndYearAsync(activeEmployeeIds, year)).ToList();
            var allocationMap = allAllocations.ToLookup(a => a.EmployeeId);

            var allocationsToUpsert = new List<LeaveAllocation>();

            foreach (var emp in activeEmployees)
            {
                var empAllocations = allocationMap[emp.Id].ToDictionary(a => a.LeaveTypeId);

                foreach (var type in accrualTypes)
                {
                    if (!empAllocations.TryGetValue(type.Id, out var allocation))
                    {
                        allocation = new LeaveAllocation(emp.Id, type.Id, year, 0);
                        allocation.SetId(Guid.NewGuid().ToString());
                    }

                    if (allocation.LastAccrualMonth == currentMonthKey)
                    {
                        continue;
                    }

                    var newAccrued = allocation.AccruedDays + type.AccrualRatePerMonth;
                    allocation.UpdateAccrual(newAccrued, currentMonthKey);

                    allocationsToUpsert.Add(allocation);
                }
            }

            // 2. Bulk Upsert all modified/new allocations
            await _allocationRepo.BulkUpsertAsync(allocationsToUpsert);
        }

        public async Task UpdateUsedDaysAsync(string employeeId, string leaveTypeId, string year, double days)
        {
            var allocation = await _allocationRepo.GetByEmployeeAndTypeAsync(employeeId, leaveTypeId, year);
            if (allocation != null)
            {
                allocation.UpdateUsedDays(days);
                await _allocationRepo.UpdateAsync(allocation.Id, allocation);
            }
        }

        public async Task RefundDaysAsync(string employeeId, string leaveTypeId, string year, double days)
        {
            var allocation = await _allocationRepo.GetByEmployeeAndTypeAsync(employeeId, leaveTypeId, year);
            if (allocation != null)
            {
                allocation.RefundUsage(days);
                await _allocationRepo.UpdateAsync(allocation.Id, allocation);
            }
        }

        /// <summary>
        /// NEW-5: Year-end carry forward.
        /// For each active/probation employee and each leave type with AllowCarryForward=true,
        /// carry min(unusedBalance, MaxCarryForwardDays) into next year's allocation.
        /// Returns the number of allocation records created/updated.
        /// </summary>
        public async Task<int> RunYearEndCarryForwardAsync(int fromYear)
        {
            var fromYearStr = fromYear.ToString();
            var toYearStr = (fromYear + 1).ToString();

            // 1. Load leave types that allow carry-forward
            var leaveTypesPaged = await _leaveTypeRepo.GetPagedAsync(new PaginationParams { PageSize = 100 });
            var carryForwardTypes = leaveTypesPaged.Items
                .Where(t => t.AllowCarryForward && t.MaxCarryForwardDays > 0)
                .ToList();

            if (!carryForwardTypes.Any()) return 0;

            // 2. Load eligible employees (Active + Probation)
            var allEmployees = (await _employeeRepo.GetAllAsync())
                .Where(e => e.JobDetails != null &&
                            e.JobDetails.Status != EmployeeStatus.Terminated &&
                            e.JobDetails.Status != EmployeeStatus.Resigned)
                .ToList();

            if (!allEmployees.Any()) return 0;

            var employeeIds = allEmployees.Select(e => e.Id).ToList();

            // 3. Bulk-fetch both year allocations
            var fromAllocations = (await _allocationRepo.GetByEmployeeIdsAndYearAsync(employeeIds, fromYearStr)).ToList();
            var toAllocations = (await _allocationRepo.GetByEmployeeIdsAndYearAsync(employeeIds, toYearStr)).ToList();

            var toAllocMap = toAllocations.ToDictionary(a => (a.EmployeeId, a.LeaveTypeId));
            var toUpsert = new List<LeaveAllocation>();

            foreach (var leaveType in carryForwardTypes)
            {
                foreach (var emp in allEmployees)
                {
                    var fromAlloc = fromAllocations.FirstOrDefault(
                        a => a.EmployeeId == emp.Id && a.LeaveTypeId == leaveType.Id);

                    if (fromAlloc == null) continue;

                    var unusedBalance = fromAlloc.CurrentBalance;
                    if (unusedBalance <= 0) continue;

                    var carryDays = Math.Min(unusedBalance, leaveType.MaxCarryForwardDays);

                    if (toAllocMap.TryGetValue((emp.Id, leaveType.Id), out var toAlloc))
                    {
                        // Add carry-forward on top of existing allocation for next year
                        toAlloc.UpdateAllocation(toAlloc.NumberOfDays + carryDays);
                    }
                    else
                    {
                        toAlloc = new LeaveAllocation(emp.Id, leaveType.Id, toYearStr, carryDays);
                        toAlloc.SetId(Guid.NewGuid().ToString());
                        toAllocMap[(emp.Id, leaveType.Id)] = toAlloc; // avoid duplicates
                    }

                    toUpsert.Add(toAlloc);
                }
            }

            if (toUpsert.Any())
                await _allocationRepo.BulkUpsertAsync(toUpsert);

            return toUpsert.Count;
        }
    }
}
