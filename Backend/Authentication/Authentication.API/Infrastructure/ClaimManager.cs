using System.Security.Claims;
using Authentication.DAL.Models;
using Authentication.DL.Repositories;
using Authentication.DL.Services;


namespace Authentication.API.Infrastructure;

public class ClaimManager : IClaimManager
{
    private readonly IUsersService _usersService;
    private readonly ILogger<ClaimManager> _logger;

    public ClaimManager(IUsersService usersRepository, ILogger<ClaimManager> logger)
    {
        _usersService = usersRepository;
        _logger = logger;
    }
    
    public async Task<bool> AddClaimToUser(string email, string claimName, string value)
    {
        var user = _usersService.GetByEmail(email);

        var userClaim = new Claim(claimName, value);

        if (user != null)
        {
            var result = await _usersService.AddClaimAsync(user, userClaim);
            
            if(result.Succeeded)
            {
                _logger.LogInformation (1, $"the claim {claimName} add to the  User {user.Email}");
                return false;
            }
            else
            {
                _logger.LogInformation (1, $"Error: Unable to add the claim {claimName} to the  User {user.Email}");
                return false;
            }
        }

        return false;
    }
}