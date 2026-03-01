using Employee.Domain.Entities.Leave;
using Employee.Domain.Enums;
using Xunit;

namespace Employee.UnitTests.Domain.Entities.Leave
{
    public class LeaveRequestTests
    {
        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange
            var from = DateTime.UtcNow.AddDays(1);
            var to = from.AddDays(2);

            // Act
            var request = new LeaveRequest("emp1", LeaveCategory.Annual, from, to, "Vacation");

            // Assert
            Assert.Equal("emp1", request.EmployeeId);
            Assert.Equal(LeaveStatus.Pending, request.Status);
            Assert.Equal(from, request.FromDate);
            Assert.Equal(to, request.ToDate);
        }

        [Theory]
        [InlineData("", "Vacation")]
        [InlineData("emp1", "")]
        [InlineData("emp1", "Too long reason...")] // See next test for specific length check
        public void Constructor_InvalidInput_ShouldThrowException(string? empId, string? reason)
        {
            var from = DateTime.UtcNow;
            var to = from.AddDays(1);

            if (reason == "Too long reason...")
            {
                reason = new string('a', 301);
            }

            Assert.Throws<ArgumentException>(() => new LeaveRequest(empId!, LeaveCategory.Annual, from, to, reason!));
        }

        [Fact]
        public void Constructor_ToDateBeforeFromDate_ShouldThrowException()
        {
            var from = DateTime.UtcNow.AddDays(1);
            var to = from.AddDays(-1);

            Assert.Throws<ArgumentException>(() => new LeaveRequest("emp1", LeaveCategory.Annual, from, to, "Reason"));
        }

        [Fact]
        public void Approve_WhenPending_ShouldSucceed()
        {
            // Arrange
            var request = new LeaveRequest("emp1", LeaveCategory.Annual, DateTime.UtcNow, DateTime.UtcNow, "Reason");

            // Act
            request.Approve("manager1", "Enjoy your vacation");

            // Assert
            Assert.Equal(LeaveStatus.Approved, request.Status);
            Assert.Equal("manager1", request.ApprovedBy);
            Assert.Equal("Enjoy your vacation", request.ManagerComment);
        }

        [Fact]
        public void Approve_WhenAlreadyApproved_ShouldThrowException()
        {
            // Arrange
            var request = new LeaveRequest("emp1", LeaveCategory.Annual, DateTime.UtcNow, DateTime.UtcNow, "Reason");
            request.Approve("mgr1", "ok");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => request.Approve("mgr2", "duplicate"));
        }

        [Fact]
        public void Reject_WhenPending_ShouldSucceed()
        {
            // Arrange
            var request = new LeaveRequest("emp1", LeaveCategory.Annual, DateTime.UtcNow, DateTime.UtcNow, "Reason");

            // Act
            request.Reject("manager1", "Too many people off");

            // Assert
            Assert.Equal(LeaveStatus.Rejected, request.Status);
            Assert.Equal("manager1", request.ApprovedBy); // Store who rejected
            Assert.Equal("Too many people off", request.ManagerComment);
        }

        [Fact]
        public void Update_WhenPending_ShouldSucceed()
        {
            // Arrange
            var request = new LeaveRequest("emp1", LeaveCategory.Annual, DateTime.UtcNow, DateTime.UtcNow, "Old Reason");
            var newTo = DateTime.UtcNow.AddDays(5);

            // Act
            request.Update(LeaveCategory.Sick, DateTime.UtcNow, newTo, "New Reason", System.DateTime.UtcNow);

            // Assert
            Assert.Equal(LeaveCategory.Sick, request.LeaveType);
            Assert.Equal("New Reason", request.Reason);
            Assert.Equal(newTo, request.ToDate);
        }

        [Fact]
        public void Update_WhenNotPending_ShouldThrowException()
        {
            // Arrange
            var request = new LeaveRequest("emp1", LeaveCategory.Annual, DateTime.UtcNow, DateTime.UtcNow, "Reason");
            request.Approve("mgr", "ok");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => request.Update(LeaveCategory.Annual, DateTime.UtcNow, DateTime.UtcNow, "Update", System.DateTime.UtcNow));
        }

        [Fact]
        public void Cancel_ShouldSetStatusToCancelled()
        {
            // Arrange
            var request = new LeaveRequest("emp1", LeaveCategory.Annual, DateTime.UtcNow, DateTime.UtcNow, "Reason");

            // Act
            request.Cancel(System.DateTime.UtcNow);

            // Assert
            Assert.Equal(LeaveStatus.Cancelled, request.Status);
        }
    }
}

