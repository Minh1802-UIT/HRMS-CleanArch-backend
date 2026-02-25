namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface IPayrollProcessingService
  {
    Task<int> CalculatePayrollAsync(string month, string year);
    Task FinalizePayrollAsync(string month, string year);
  }
}
