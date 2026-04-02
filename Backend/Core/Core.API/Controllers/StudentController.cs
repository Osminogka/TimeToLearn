using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DL.Services;

namespace Core.API.Controllers;

[Route("api/u/[controller]")]
[Authorize]
public class StudentController : BaseController
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(IStudentService studentService, ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [HttpPost("become")]
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
    
    [HttpPost("request/{universityName}")]
    public async Task<IActionResult> SendRequestToBecomeStudentOfUniversity(string universityName)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _studentService.SendRequestToBecomeStudentOfUniversity(universityName, getUserEmail());
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
    
    [HttpPost("entry/{universityName}")]
    public async Task<IActionResult> EntryUniversityAsync(string universityName)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _studentService.EntryUniversityAsync(universityName, getUserEmail());
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