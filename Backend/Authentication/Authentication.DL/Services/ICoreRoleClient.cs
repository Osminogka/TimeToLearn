namespace Authentication.DL.Services
{
    public interface ICoreRoleClient
    {
        /// <summary>
        /// Returns the role name (Student/Teacher) for the given email.
        /// Falls back to Student if Core is unavailable or the user is not found yet.
        /// </summary>
        Task<string> GetRoleAsync(string email);
    }
}
