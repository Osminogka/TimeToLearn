using Microsoft.AspNetCore.Mvc;
using Users.DL.Services;

namespace Users.API.Controllers;

[Route("api/u/[controller]")]
public class TeacherController : BaseController
{
    private readonly ITeacherService _teacherService;
    private readonly ILogger<TeacherController> _logger;

    public TeacherController(ITeacherService teacherService, ILogger<TeacherController> logger)
    {
        _teacherService = teacherService;
        _logger = logger;
    }
    
    [HttpGet("become")]
    public async Task<IActionResult> BecomeTeacherAsync()
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _teacherService.BecomeTeacherAsync(getUserEmail());
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
    
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyStatusAsync([FromBody] string degree)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _teacherService.VerifyStatusAsync(getUserEmail(), degree);
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
    public async Task<IActionResult> SendRequestToBecomeTeacherOfUniversityAsync(string universityName)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");
            var result = await _teacherService.SendRequestToBecomeTeacherOfUniversity(universityName, getUserEmail());
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