using Kaizen.Api.Services.Abstractions;

namespace Kaizen.Api.Services.Auth;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 12);

    public bool Verify(string senha, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(senha, hash); }
        catch { return false; }
    }
}
