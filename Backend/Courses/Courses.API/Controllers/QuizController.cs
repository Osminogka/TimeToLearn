using Courses.DAL.Dtos;
using Courses.DL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Courses.API.Controllers
{
    [Route("api/c/quizzes")]
    [Authorize]
    public class QuizController : BaseController
    {
        private readonly IQuizService _quizService;
        private readonly ILogger<QuizController> _logger;

        public QuizController(IQuizService quizService, ILogger<QuizController> logger)
        {
            _quizService = quizService;
            _logger = logger;
        }

        [HttpGet("lesson/{lessonId}")]
        public async Task<IActionResult> GetLessonQuizQuestionsAsync(long lessonId)
        {
            try
            {
                var result = await _quizService.GetLessonQuizQuestionsAsync(lessonId, getUserEmail());
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
        public async Task<IActionResult> GetCourseQuizQuestionsAsync(long courseId)
        {
            try
            {
                var result = await _quizService.GetCourseQuizQuestionsAsync(courseId, getUserEmail());
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

        [HttpPost("lesson/{lessonId}/questions")]
        public async Task<IActionResult> CreateLessonQuizQuestionAsync(long lessonId, CreateQuizQuestionDto dto)
        {
            try
            {
                var result = await _quizService.CreateLessonQuizQuestionAsync(lessonId, dto, getUserEmail());
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

        [HttpPost("course/{courseId}/questions")]
        public async Task<IActionResult> CreateCourseQuizQuestionAsync(long courseId, CreateQuizQuestionDto dto)
        {
            try
            {
                var result = await _quizService.CreateCourseQuizQuestionAsync(courseId, dto, getUserEmail());
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

        [HttpPost("lesson/{lessonId}/questions/{questionId}/answer")]
        public async Task<IActionResult> SubmitLessonQuizAnswerAsync(long lessonId, long questionId, SubmitQuizAnswerDto dto)
        {
            try
            {
                var result = await _quizService.SubmitLessonQuizAnswerAsync(lessonId, questionId, dto, getUserEmail());
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

        [HttpPost("course/{courseId}/questions/{questionId}/answer")]
        public async Task<IActionResult> SubmitCourseQuizAnswerAsync(long courseId, long questionId, SubmitQuizAnswerDto dto)
        {
            try
            {
                var result = await _quizService.SubmitCourseQuizAnswerAsync(courseId, questionId, dto, getUserEmail());
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

        [HttpGet("lesson/{lessonId}/my-answers")]
        public async Task<IActionResult> GetMyLessonQuizAnswersAsync(long lessonId)
        {
            try
            {
                var result = await _quizService.GetMyLessonQuizAnswersAsync(lessonId, getUserEmail());
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

        [HttpGet("course/{courseId}/my-answers")]
        public async Task<IActionResult> GetMyCourseQuizAnswersAsync(long courseId)
        {
            try
            {
                var result = await _quizService.GetMyCourseQuizAnswersAsync(courseId, getUserEmail());
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

        [HttpGet("lesson/{lessonId}/answers")]
        public async Task<IActionResult> GetLessonQuizAnswersForTeacherAsync(long lessonId)
        {
            try
            {
                var result = await _quizService.GetLessonQuizAnswersForTeacherAsync(lessonId, getUserEmail());
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

        [HttpGet("course/{courseId}/answers")]
        public async Task<IActionResult> GetCourseQuizAnswersForTeacherAsync(long courseId)
        {
            try
            {
                var result = await _quizService.GetCourseQuizAnswersForTeacherAsync(courseId, getUserEmail());
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
