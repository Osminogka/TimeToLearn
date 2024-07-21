using Microsoft.AspNetCore.Mvc;
using Users.DL.Services;

namespace Users.API.Controllers;

[Route("api/[controller]")]
public class StudentController : BaseController
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(IStudentService studentService, ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [HttpGet("become")]
    public async Task<IActionResult> BecomeAStudentAsync()
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _studentService.BecomeAStudentAsync(getUserEmail());
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return HandleException(ex);
        }
    }
    
    [HttpGet("become")]
    public async Task<IActionResult> InviteStudentToUniversity()
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _studentService.BecomeAStudentAsync(getUserEmail());
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return HandleException(ex);
        }
    }
    
    [HttpGet("become")]
    public async Task<IActionResult> SendRequestToBecomeStudentOfUniversity()
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _studentService.BecomeAStudentAsync(getUserEmail());
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return HandleException(ex);
        }
    }
    
    [HttpGet("become")]
    public async Task<IActionResult> EntryUniversityAsync()
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _studentService.BecomeAStudentAsync(getUserEmail());
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return HandleException(ex);
        }
    }
}