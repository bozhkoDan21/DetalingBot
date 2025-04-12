public interface IAuthService
{
    string GenerateJwtToken(int userId);
}