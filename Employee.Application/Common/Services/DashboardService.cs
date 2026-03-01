using Employee.Application.Common.Dtos;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Employee.Domain.Common.Models;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using System;

namespace Employee.Application.Common.Services
{
  public class DashboardService : IDashboardService
  {
    private readonly IEnumerable<IDashboardProvider> _providers;

    public DashboardService(IEnumerable<IDashboardProvider> providers)
    {
      _providers = providers;
    }

    public async Task<DashboardDto> GetDashboardDataAsync()
    {
      var dto = new DashboardDto();

      foreach (var provider in _providers)
      {
        await provider.PopulateDashboardAsync(dto);
      }

      // Sort summary cards if needed (e.g., by title or a fixed order)
      dto.SummaryCards = dto.SummaryCards.OrderBy(c => c.Title).ToList();

      return dto;
    }
  }
}
