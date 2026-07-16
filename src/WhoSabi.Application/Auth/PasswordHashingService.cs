namespace WhoSabi.Application.Auth;

public interface IPasswordHashingService
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
