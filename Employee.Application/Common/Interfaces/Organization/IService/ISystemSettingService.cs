using System.Collections.Generic;
using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface ISystemSettingService
  {
    Task<string> GetStringAsync(string key, string defaultValue = "");
    Task<Dictionary<string, string>> GetMultipleAsync(IEnumerable<string> keys);
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0);
    Task<int> GetIntAsync(string key, int defaultValue = 0);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task SetAsync(string key, string value, string group = "General", string description = "");
  }
}
