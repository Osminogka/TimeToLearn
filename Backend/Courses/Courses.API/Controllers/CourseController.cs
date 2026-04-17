using Courses.DAL.Dtos;
using Courses.DL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courses.API.Controllers
{
    [Route("api/c/courses")]
    [Authorize]
    public class CourseController : BaseController
    {
        private readonly ICourseService _courseService;
        private readonly IProgressService _progressService;
        private readonly ILogger<CourseController> _logger;

        public CourseController(ICourseService courseService, IProgressService progressService, ILogger<CourseController> logger)
        {
            _courseService = courseService;
            _progressService = progressService;
            _logger = logger;
        }

        [HttpGet("{universityName}/{page}")]
        public async Task<IActionResult> GetUniversityCoursesAsync(string universityName, int page)
        {
            try
            {
                var result = await _courseService.GetUniversityCoursesAsync(universityName, getUserEmail(), page);
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

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseAsync(long courseId)
        {
            try
            {
                var result = await _courseService.GetCourseAsync(courseId, getUserEmail());
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
        public async Task<IActionResult> CreateCourseAsync(CreateCourseDto course)
        {
            try
            {
                var result = await _courseService.CreateCourseAsync(course, getUserEmail());
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
        public async Task<IActionResult> UpdateCourseAsync(UpdateCourseDto course)
        {
            try
            {
                var result = await _courseService.UpdateCourseAsync(course, getUserEmail());
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

        [HttpDelete("delete/{courseId}")]
        public async Task<IActionResult> DeleteCourseAsync(long courseId)
        {
            try
            {
                var result = await _courseService.DeleteCourseAsync(courseId, getUserEmail());
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

        [HttpGet("{courseId}/progress")]
        public async Task<IActionResult> GetCourseProgressAsync(long courseId)
        {
            try
            {
                var result = await _progressService.GetCourseProgressAsync(courseId, getUserEmail());
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

        [HttpGet("{courseId}/student-progress")]
        public async Task<IActionResult> GetCourseStudentsProgressAsync(long courseId)
        {
            try
            {
                var result = await _progressService.GetCourseStudentsProgressAsync(courseId, getUserEmail());
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

        [HttpPost("{courseId}/grade-student")]
        public async Task<IActionResult> AssignCourseGradeAsync(long courseId, AssignCourseGradeDto gradeDto)
        {
            try
            {
                var result = await _progressService.AssignCourseGradeAsync(courseId, gradeDto, getUserEmail());
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
