using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body, bool isHtml = false);
    }
}
