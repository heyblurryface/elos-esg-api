using Kaizen.Api.Common.Exceptions;
using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Auth;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly KaizenDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasher _hasher;

    public AuthService(KaizenDbContext db, IJwtService jwt, IPasswordHasher hasher)
    {
        _db = db; _jwt = jwt; _hasher = hasher;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var consultor = await _db.Consultores
            .FirstOrDefaultAsync(c => c.ConEmail.ToLower() == email, ct);

        if (consultor is null || !_hasher.Verify(req.Senha, consultor.ConSenha))
            throw new UnauthorizedAppException("E-mail ou senha inválidos.");

        return Build(consultor);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Consultores.AnyAsync(c => c.ConEmail.ToLower() == email, ct))
            throw new ConflictException("Já existe um consultor com este e-mail.");

        var consultor = new Consultor
        {
            ConId = await NextIdAsync(ct),
            ConNome = req.Nome.Trim(),
            ConEmail = email,
            ConSenha = _hasher.Hash(req.Senha),
            ConFoto = req.Foto,
            ConDtCadastro = DateTime.UtcNow
        };

        _db.Consultores.Add(consultor);
        await _db.SaveChangesAsync(ct);
        return Build(consultor);
    }

    private async Task<int> NextIdAsync(CancellationToken ct) =>
        await _db.Consultores.AnyAsync(ct) ? await _db.Consultores.MaxAsync(c => c.ConId, ct) + 1 : 1;

    private AuthResponse Build(Consultor c)
    {
        var (token, exp) = _jwt.GerarToken(c.ConId, c.ConNome, c.ConEmail);
        return new AuthResponse
        {
            Token = token,
            ExpiraEm = exp,
            ConsultorId = c.ConId,
            Nome = c.ConNome,
            Email = c.ConEmail
        };
    }
}
