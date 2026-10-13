using Kaizen.Api.Common.Exceptions;
using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Empresas;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Services;

public class EmpresaService : IEmpresaService
{
    private readonly KaizenDbContext _db;
    public EmpresaService(KaizenDbContext db) => _db = db;

    public async Task<PagedResult<EmpresaResponse>> ListarAsync(PageQuery page, string? setor, CancellationToken ct = default)
    {
        var query = _db.Empresas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(setor))
        {
            var s = setor.Trim().ToLower();
            query = query.Where(e => e.EmpSetor != null && e.EmpSetor.ToLower().Contains(s));
        }
        var ordered = query.OrderBy(e => e.EmpId);
        var paged = await PagedResult<Empresa>.CreateAsync(ordered, page.Page, page.PageSize, ct);
        return paged.Map(Map);
    }

    public async Task<EmpresaResponse> ObterAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(x => x.EmpId == id, ct)
                ?? throw new NotFoundException($"Empresa {id} não encontrada.");
        return Map(e);
    }

    public async Task<EmpresaResponse> CriarAsync(EmpresaRequest req, CancellationToken ct = default)
    {
        var e = new Empresa
        {
            EmpId = await NextIdAsync(ct),
            EmpDtCadastro = DateTime.UtcNow
        };
        Apply(e, req);
        _db.Empresas.Add(e);
        await _db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<EmpresaResponse> AtualizarAsync(int id, EmpresaRequest req, CancellationToken ct = default)
    {
        var e = await _db.Empresas.FirstOrDefaultAsync(x => x.EmpId == id, ct)
                ?? throw new NotFoundException($"Empresa {id} não encontrada.");
        Apply(e, req);
        await _db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.Empresas.FirstOrDefaultAsync(x => x.EmpId == id, ct)
                ?? throw new NotFoundException($"Empresa {id} não encontrada.");
        if (await _db.Avaliacoes.AnyAsync(a => a.AvaEmpId == id, ct))
            throw new ConflictException("Empresa possui avaliações vinculadas e não pode ser removida.");
        _db.Empresas.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> NextIdAsync(CancellationToken ct) =>
        await _db.Empresas.AnyAsync(ct) ? await _db.Empresas.MaxAsync(e => e.EmpId, ct) + 1 : 1;

    private static void Apply(Empresa e, EmpresaRequest r)
    {
        e.EmpNome = r.Nome.Trim();
        e.EmpSetor = r.Setor;
        e.EmpPorte = r.Porte;
        e.EmpNumFunc = r.NumeroFuncionarios;
        e.EmpResp = r.Responsavel;
        e.EmpRespCargo = r.ResponsavelCargo;
        e.EmpRespAreas = r.ResponsavelAreas;
        e.EmpGestTerc = string.IsNullOrWhiteSpace(r.GestaoTerceiros) ? "I" : r.GestaoTerceiros.ToUpper();
        e.EmpFoto = r.Foto;
    }

    private static EmpresaResponse Map(Empresa e) => new()
    {
        Id = e.EmpId,
        Nome = e.EmpNome,
        Setor = e.EmpSetor,
        Porte = e.EmpPorte,
        NumeroFuncionarios = e.EmpNumFunc,
        Responsavel = e.EmpResp,
        ResponsavelCargo = e.EmpRespCargo,
        ResponsavelAreas = e.EmpRespAreas,
        GestaoTerceiros = e.EmpGestTerc,
        Foto = e.EmpFoto,
        DataCadastro = e.EmpDtCadastro
    };
}
