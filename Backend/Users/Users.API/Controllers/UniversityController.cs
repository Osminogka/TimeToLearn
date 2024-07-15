using Microsoft.AspNetCore.Mvc;
using Users.DAL.Dtos;
using Users.DAL.SideModels;
using Users.DL.Services;

namespace Users.API.Controllers
{
    [Route("api/[controller]/")]
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
        public async Task<IActionResult> GetAllUniversitiesAsync()
        {
            try
            {
                var result = await _universityService.GetAllAsync();
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
        public async Task<IActionResult> GetUniversityByName(string name)
        {
            try
            {
                var result = await _universityService.GetAsync(name);
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
        public async Task<IActionResult> CreateUniversity([FromBody] CreateUniversityDto model)
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
        public async Task<IActionResult> GetUniversityTeachers(string name)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid request");
                var result = await _universityService.GetTeachersAsync(name, getUserEmail());
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
        public async Task<IActionResult> GetUniversityStudents(string name)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid request");
                var result = await _universityService.GetStudentsAsync(name, getUserEmail());
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