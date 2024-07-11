using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Users.API.Controllers
{
    [ApiController]
    [Authorize]
    public class BaseController : ControllerBase
    {
        protected string getUserEmail()
        {
            return HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;
        }

        protected IActionResult HandleException(Exception ex)
        {
            if (ex is ArgumentNullException)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the request.");
            else if (ex is InvalidOperationException)
                return StatusCode(StatusCodes.Status500InternalServerError, "More than one element satisfies the condition in SingleOrDefault.");
            else if (ex is DbUpdateException)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the database.");
            else
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }
}
