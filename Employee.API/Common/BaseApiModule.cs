using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Common
{
  [ApiController]
  public class BaseApiModule : ControllerBase
  {
    protected IActionResult Success<T>(T data, string message = "")
    {
      return Ok(new { Succeeded = true, Message = message, Data = data });
    }

    protected IActionResult Success(string message)
    {
      return Ok(new { Succeeded = true, Message = message });
    }
  }
}
