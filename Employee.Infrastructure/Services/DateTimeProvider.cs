using System;
using Employee.Domain.Interfaces.Common;

namespace Employee.Infrastructure.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
