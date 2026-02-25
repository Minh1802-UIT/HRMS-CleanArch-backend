namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface IAttendanceProcessingService
  {
    Task<string> ProcessRawLogsAsync();
  }
}
