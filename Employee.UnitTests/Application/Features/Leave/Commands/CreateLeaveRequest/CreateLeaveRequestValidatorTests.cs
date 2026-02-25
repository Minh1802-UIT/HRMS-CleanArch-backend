using Employee.Application.Features.Leave.Commands.CreateLeaveRequest;
using FluentValidation.TestHelper;
using Xunit;

namespace Employee.UnitTests.Application.Features.Leave.Commands.CreateLeaveRequest
{
    public class CreateLeaveRequestValidatorTests
    {
        private readonly CreateLeaveRequestValidator _validator;

        public CreateLeaveRequestValidatorTests()
        {
            _validator = new CreateLeaveRequestValidator();
        }

        [Fact]
        public void Should_Have_Error_When_LeaveType_Is_Empty()
        {
            var command = new CreateLeaveRequestCommand { LeaveType = "" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.LeaveType);
        }

        [Fact]
        public void Should_Have_Error_When_LeaveType_Is_Invalid()
        {
            var command = new CreateLeaveRequestCommand { LeaveType = "InvalidType" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.LeaveType);
        }

        [Fact]
        public void Should_Not_Have_Error_When_LeaveType_Is_Valid()
        {
            var command = new CreateLeaveRequestCommand { LeaveType = "Annual" };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveValidationErrorFor(x => x.LeaveType);
        }

        [Fact]
        public void Should_Have_Error_When_FromDate_Is_Empty()
        {
            var command = new CreateLeaveRequestCommand { FromDate = default };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.FromDate);
        }

        [Fact]
        public void Should_Have_Error_When_ToDate_Is_Before_FromDate()
        {
            var command = new CreateLeaveRequestCommand 
            { 
                FromDate = DateTime.UtcNow.AddDays(1), 
                ToDate = DateTime.UtcNow 
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ToDate)
                  .WithErrorMessage("End Date must be after Start Date");
        }

        [Fact]
        public void Should_Have_Error_When_Reason_Is_Too_Long()
        {
            var command = new CreateLeaveRequestCommand { Reason = new string('a', 301) };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Command_Is_Valid()
        {
            var command = new CreateLeaveRequestCommand
            {
                LeaveType = "Annual",
                FromDate = DateTime.UtcNow,
                ToDate = DateTime.UtcNow.AddDays(1),
                Reason = "Valid Reason"
            };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
