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
        private readonly IProgressService _progressService;
        private readonly ILogger<LessonController> _logger;

        public LessonController(ILessonService lessonService, IProgressService progressService, ILogger<LessonController> logger)
        {
            _lessonService = lessonService;
            _progressService = progressService;
            _logger = logger;
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseLessonsAsync(long courseId)
        {
            try
            {
                var result = await _lessonService.GetCourseLessonsAsync(courseId, getUserEmail());
                if (!result.Success)
                    return result.Message.Contains("doesn't exist") ? NotFound(result.Message) : Forbid();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }

        [HttpGet("lesson/{lessonId}")]
        public async Task<IActionResult> GetLessonAsync(long lessonId)
        {
            try
            {
                var result = await _lessonService.GetLessonAsync(lessonId, getUserEmail());
                if (!result.Success)
                    return result.Message.Contains("doesn't exist") ? NotFound(result.Message) : Forbid();
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

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderLessonsAsync(ReorderLessonsDto reorderDto)
        {
            try
            {
                var result = await _lessonService.ReorderLessonsAsync(reorderDto, getUserEmail());
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

        [HttpPost("{lessonId}/complete")]
        public async Task<IActionResult> CompleteLessonAsync(long lessonId)
        {
            try
            {
                var result = await _progressService.CompleteLessonAsync(lessonId, getUserEmail());
                if (!result.Success)
                    return result.Message.Contains("doesn't exist") ? NotFound(result.Message) : BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }

        [HttpGet("{lessonId}/progress")]
        public async Task<IActionResult> GetLessonProgressAsync(long lessonId)
        {
            try
            {
                var result = await _progressService.GetLessonProgressAsync(lessonId, getUserEmail());
                if (!result.Success)
                    return result.Message.Contains("doesn't exist") ? NotFound(result.Message) : Forbid();
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
