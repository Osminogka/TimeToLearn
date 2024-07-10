namespace Authentication.API.Infrastructure;

public interface IClaimManager
{
    Task<bool> AddClaimToUser(string email, string claimName, string value);
}