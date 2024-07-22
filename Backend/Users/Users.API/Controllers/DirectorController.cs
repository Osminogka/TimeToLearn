using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Users.DAL.SideModels;
using Users.DL.Services;

namespace Users.API.Controllers
{
    [Route("api/u/[controller]/")]
    public class DirectorController : BaseController
    {
        private readonly IDirectorService _directorService;
        private readonly ILogger<DirectorController> _logger;

        public DirectorController(IDirectorService directorService, ILogger<DirectorController> logger)
        {
            _directorService = directorService;
            _logger = logger;
        }

        [HttpPost("invites/student")]
        public async Task<IActionResult> SentInviteToStudentAsync([FromBody] EntryRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid data");
                var result = await _directorService.InviteStudentToUniversityAsync(model.University, model.Username, getUserEmail());
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
        
        [HttpPost("invites/teacher")]
        public async Task<IActionResult> SentInviteToTeacherAsync([FromBody] EntryRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid data");
                var result = await _directorService.InviteTeacherToUniversityAsync(model.University, model.Username, getUserEmail());
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
        
        [HttpPost("invites/approve")]
        public async Task<IActionResult> AcceptEntryRequestAsync([FromBody] EntryRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid data");
                var result = await _directorService.AcceptEntryRequestAsync(model, getUserEmail());
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
        
        [HttpPost("invites/reject")]
        public async Task<IActionResult> RejectEntryRequestAsync([FromBody] EntryRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid data");
                var result = await _directorService.RejectEntryRequestAsync(model, getUserEmail());
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
        
        [HttpPost("update")]
        public async Task<IActionResult> UpdateUniversityInfoAsync([FromBody] UpdateUniversityInfoModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid data");
                var result = await _directorService.UpdateUniversityInfoAsync(model, getUserEmail());
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
