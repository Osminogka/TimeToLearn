using Forums.DAL.Dtos;
using Forums.DL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forums.API.Controllers
{
    [Route("api/f/comments")]
    [Authorize]
    public class CommentController : BaseController
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentService commentService, ILogger<CommentController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        [HttpGet("{isTopic}/{recordId}/{page}")]
        public async Task<IActionResult> GetCommentsAsync(bool isTopic, long recordId, int page)
        {
            try
            {
                var result = await _commentService.GetCommentsAsync(isTopic, recordId, getUserEmail(), page);
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
        public async Task<IActionResult> CreateCommentAsync(CreateCommentDto createCommentDto)
        {
            try
            {
                var result = await _commentService.CreateCommentAsync(createCommentDto, getUserEmail());
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

        [HttpPost("like/{commentId}")]
        public async Task<IActionResult> LikeCommentAsync(long commentId)
        {
            try
            {
                var result = await _commentService.LikeCommentAsync(commentId, getUserEmail());
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

        [HttpPost("dislike/{commentId}")]
        public async Task<IActionResult> DislikeCommentAsync(long commentId)
        {
            try
            {
                var result = await _commentService.DislikeCommentAsync(commentId, getUserEmail());
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
