using Kaizen.Api.Common.Exceptions;
using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Consultores;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Services;

public class ConsultorService : IConsultorService
{
    private readonly KaizenDbContext _db;
    private readonly IPasswordHasher _hasher;

    public ConsultorService(KaizenDbContext db, IPasswordHasher hasher)
    {
        _db = db; _hasher = hasher;
    }

    public async Task<PagedResult<ConsultorResponse>> ListarAsync(PageQuery page, CancellationToken ct = default)
    {
        var query = _db.Consultores.AsNoTracking().OrderBy(c => c.ConId);
        var paged = await PagedResult<Consultor>.CreateAsync(query, page.Page, page.PageSize, ct);
        return paged.Map(Map);
    }

    public async Task<ConsultorResponse> ObterAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Consultores.AsNoTracking().FirstOrDefaultAsync(x => x.ConId == id, ct)
                ?? throw new NotFoundException($"Consultor {id} não encontrado.");
        return Map(c);
    }

    public async Task<ConsultorResponse> CriarAsync(ConsultorRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Consultores.AnyAsync(c => c.ConEmail.ToLower() == email, ct))
            throw new ConflictException("Já existe um consultor com este e-mail.");

        if (string.IsNullOrWhiteSpace(req.Senha))
            throw new BusinessRuleException("Senha é obrigatória ao criar um consultor.");

        var c = new Consultor
        {
            ConId = await NextIdAsync(ct),
            ConNome = req.Nome.Trim(),
            ConEmail = email,
            ConSenha = _hasher.Hash(req.Senha!),
            ConFoto = req.Foto,
            ConDtCadastro = DateTime.UtcNow
        };
        _db.Consultores.Add(c);
        await _db.SaveChangesAsync(ct);
        return Map(c);
    }

    public async Task<ConsultorResponse> AtualizarAsync(int id, ConsultorRequest req, CancellationToken ct = default)
    {
        var c = await _db.Consultores.FirstOrDefaultAsync(x => x.ConId == id, ct)
                ?? throw new NotFoundException($"Consultor {id} não encontrado.");

        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Consultores.AnyAsync(x => x.ConEmail.ToLower() == email && x.ConId != id, ct))
            throw new ConflictException("Já existe outro consultor com este e-mail.");

        c.ConNome = req.Nome.Trim();
        c.ConEmail = email;
        c.ConFoto = req.Foto;
        if (!string.IsNullOrWhiteSpace(req.Senha))   // senha em branco mantém a atual
            c.ConSenha = _hasher.Hash(req.Senha!);

        await _db.SaveChangesAsync(ct);
        return Map(c);
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Consultores.FirstOrDefaultAsync(x => x.ConId == id, ct)
                ?? throw new NotFoundException($"Consultor {id} não encontrado.");

        if (await _db.Avaliacoes.AnyAsync(a => a.AvaConId == id, ct))
            throw new ConflictException("Consultor possui avaliações vinculadas e não pode ser removido.");

        _db.Consultores.Remove(c);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> NextIdAsync(CancellationToken ct) =>
        await _db.Consultores.AnyAsync(ct) ? await _db.Consultores.MaxAsync(c => c.ConId, ct) + 1 : 1;

    private static ConsultorResponse Map(Consultor c) => new()
    {
        Id = c.ConId,
        Nome = c.ConNome,
        Email = c.ConEmail,
        Foto = c.ConFoto,
        DataCadastro = c.ConDtCadastro
    };
}
