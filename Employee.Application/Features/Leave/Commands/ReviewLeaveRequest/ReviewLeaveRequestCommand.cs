using Employee.Application.Common.Security;
using Employee.Application.Features.Leave.Dtos;
using MediatR;

namespace Employee.Application.Features.Leave.Commands.ReviewLeaveRequest
{
    [Authorize(Roles = "Admin,HR,Manager")]
    public class ReviewLeaveRequestCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public ReviewLeaveRequestDto ReviewDto { get; set; } = new();
        public string ApprovedBy { get; set; } = string.Empty; // From Token
        public string ApprovedByName { get; set; } = string.Empty; // From Token
        /// <summary>Expected document version for optimistic concurrency check.</summary>
        public int ExpectedVersion { get; set; } = 1;
    }
}
