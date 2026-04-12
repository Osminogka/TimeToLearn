using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DAL.Dtos;
using Core.DL.Services;

namespace Core.API.Controllers
{
    [Route("api/u/[controller]/")]
    public class UniversityController : BaseController
    {
        private readonly IUniversityService _universityService;
        private readonly ILogger<UniversityController> _logger;

        public UniversityController(IUniversityService universityService, ILogger<UniversityController> logger)
        {
            _universityService = universityService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPagedUniversitiesAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _universityService.GetPagedAsync(page, pageSize);
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

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyUniversitiesAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _universityService.GetMyUniversitiesAsync(getUserEmail(), page, pageSize);
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

        [HttpGet("catalog/available")]
        [Authorize]
        public async Task<IActionResult> GetAvailableUniversitiesAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _universityService.GetAvailableAsync(getUserEmail(), page, pageSize);
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

        [HttpGet("{name}")]
        [Authorize]
        public async Task<IActionResult> GetUniversityByNameAsync(string name)
        {
            try
            {
                var result = await _universityService.GetAsync(name, getUserEmail());
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateUniversityAsync([FromBody] CreateUniversityDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid request");
                var result = await _universityService.CreateAsync(model, getUserEmail());
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

        [HttpGet("{name}/teachers")]
        public async Task<IActionResult> GetUniversityTeachersAsync(string name, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _universityService.GetTeachersAsync(name, getUserEmail(), page, pageSize);
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

        [HttpGet("{name}/students")]
        public async Task<IActionResult> GetUniversityStudentsAsync(string name, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _universityService.GetStudentsAsync(name, getUserEmail(), page, pageSize);
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
