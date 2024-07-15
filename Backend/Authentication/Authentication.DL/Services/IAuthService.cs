using Authentication.DAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.DL.Services
{
    public interface IAuthService
    {
        Task<ResponseMessage> LoginAsync(LoginRequestModel loginModel);
        Task<ResponseMessage> RegisterAsync(RegisterRequestModel model);
    }
}
