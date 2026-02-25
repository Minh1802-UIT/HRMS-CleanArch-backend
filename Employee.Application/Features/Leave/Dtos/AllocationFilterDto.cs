using Employee.Application.Common.Models;

namespace Employee.Application.Features.Leave.Dtos
{
    public class AllocationFilterDto : PaginationParams
    {
        public string? Keyword { get; set; }
    }
}
