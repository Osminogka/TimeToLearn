using Forums.DAL.Dtos;
using Forums.DAL.Models;
using Forums.DL.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forums.API.Controllers
{
    [Route("api/f/topics")]
    public class TopicController : BaseController
    {
        private readonly ITopicService _topicService;
        private readonly ILogger<TopicController> _logger;

        public TopicController(ITopicService topicService, ILogger<TopicController> logger)
        {
            _topicService = topicService;
            _logger = logger;
        }

        [HttpGet("{universityName}/{page}")]
        public async Task<IActionResult> GetUniversityTopicsAsync(string universityName, int page)
        {
            try
            {
                var result = await _topicService.GetUniversityTopicsAsync(universityName, getUserEmail(), page);
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
        public async Task<IActionResult> CreateTopicAsync(CreateTopicDto topic)
        {
            try
            {
                var result = await _topicService.CreateTopicAsync(topic, getUserEmail());
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

        [HttpPost("like/{topicId}")]
        public async Task<IActionResult> LikeTopicAsync(long topicId)
        {
            try
            {
                var result = await _topicService.LikeTopicAsync(topicId, getUserEmail());
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

        [HttpPost("dislike/{topicId}")]
        public async Task<IActionResult> DislikeTopicAsync(long topicId)
        {
            try
            {
                var result = await _topicService.DislikeTopicAsync(topicId, getUserEmail());
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
