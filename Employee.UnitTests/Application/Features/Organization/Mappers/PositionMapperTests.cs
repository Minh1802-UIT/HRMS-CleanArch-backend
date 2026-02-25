using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Mappers;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.ValueObjects;
using Xunit;

namespace Employee.UnitTests.Application.Features.Organization.Mappers
{
    public class PositionMapperTests
    {
        [Fact]
        public void ToDto_ShouldMapCorrectly()
        {
            // Arrange
            var position = new Position("Senior Dev", "SEN-DEV", "dept-1");
            position.UpdateSalaryRange(new SalaryRange { Min = 5000, Max = 8000, Currency = "USD" });

            // Act
            var dto = position.ToDto();

            // Assert
            Assert.Equal(position.Id, dto.Id);
            Assert.Equal("Senior Dev", dto.Title);
            Assert.Equal("SEN-DEV", dto.Code);
            Assert.Equal("dept-1", dto.DepartmentId);
            Assert.NotNull(dto.SalaryRange);
            Assert.Equal(5000, dto.SalaryRange.Min);
            Assert.Equal(8000, dto.SalaryRange.Max);
            Assert.Equal("USD", dto.SalaryRange.Currency);
        }

        [Fact]
        public void ToEntity_ShouldMapCorrectly()
        {
            // Arrange
            var dto = new CreatePositionDto
            {
                Title = "Junior Dev",
                Code = "JUN-DEV",
                DepartmentId = "dept-2",
                SalaryRange = new SalaryRangeDto { Min = 2000, Max = 4000, Currency = "VND" }
            };

            // Act
            var entity = dto.ToEntity();

            // Assert
            Assert.Equal("Junior Dev", entity.Title);
            Assert.Equal("JUN-DEV", entity.Code);
            Assert.Equal("dept-2", entity.DepartmentId);
            Assert.Equal(2000, entity.SalaryRange.Min);
            Assert.Equal(4000, entity.SalaryRange.Max);
        }

        [Fact]
        public void UpdateFromDto_ShouldUpdateCorrectFields()
        {
            // Arrange
            var entity = new Position("Old Title", "OLD-CODE", "dept-1");
            var dto = new UpdatePositionDto
            {
                Title = "New Title",
                DepartmentId = "dept-new",
                SalaryRange = new SalaryRangeDto { Min = 3000, Max = 5000, Currency = "EUR" }
            };

            // Act
            entity.UpdateFromDto(dto);

            // Assert
            Assert.Equal("New Title", entity.Title);
            Assert.Equal("dept-new", entity.DepartmentId);
            Assert.Equal("OLD-CODE", entity.Code); // Should not change
            Assert.Equal(3000, entity.SalaryRange.Min);
            Assert.Equal(5000, entity.SalaryRange.Max);
        }
    }
}
