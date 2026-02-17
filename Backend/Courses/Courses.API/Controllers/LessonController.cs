using Courses.DAL.Dtos;
using Courses.DL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courses.API.Controllers
{
    [Route("api/c/lessons")]
    [Authorize]
    public class LessonController : BaseController
    {
        private readonly ILessonService _lessonService;
        private readonly ILogger<LessonController> _logger;

        public LessonController(ILessonService lessonService, ILogger<LessonController> logger)
        {
            _lessonService = lessonService;
            _logger = logger;
        }

        [HttpGet("course/{courseId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourseLessonsAsync(long courseId)
        {
            try
            {
                var result = await _lessonService.GetCourseLessonsAsync(courseId, getUserEmail());
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

        [HttpGet("lesson/{lessonId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLessonAsync(long lessonId)
        {
            try
            {
                var result = await _lessonService.GetLessonAsync(lessonId, getUserEmail());
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

        [HttpPost("create")]
        public async Task<IActionResult> CreateLessonAsync(CreateLessonDto lesson)
        {
            try
            {
                var result = await _lessonService.CreateLessonAsync(lesson, getUserEmail());
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

        [HttpPut("update")]
        public async Task<IActionResult> UpdateLessonAsync(UpdateLessonDto lesson)
        {
            try
            {
                var result = await _lessonService.UpdateLessonAsync(lesson, getUserEmail());
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

        [HttpDelete("delete/{lessonId}")]
        public async Task<IActionResult> DeleteLessonAsync(long lessonId)
        {
            try
            {
                var result = await _lessonService.DeleteLessonAsync(lessonId, getUserEmail());
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
}
