using Kaizen.Api.Common.Exceptions;
using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Recomendacoes;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Services;

public class RecomendacaoService : IRecomendacaoService
{
    private readonly KaizenDbContext _db;
    public RecomendacaoService(KaizenDbContext db) => _db = db;

    public async Task<PagedResult<RecomendacaoResponse>> ListarAsync(PageQuery page, int? resultadoId, CancellationToken ct = default)
    {
        var query = _db.Recomendacoes.AsNoTracking().AsQueryable();
        if (resultadoId is not null) query = query.Where(r => r.RecRstId == resultadoId);
        var ordered = query.OrderBy(r => r.RecId);
        var paged = await PagedResult<Recomendacao>.CreateAsync(ordered, page.Page, page.PageSize, ct);
        return paged.Map(Map);
    }

    public async Task<RecomendacaoResponse> ObterAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.Recomendacoes.AsNoTracking().FirstOrDefaultAsync(x => x.RecId == id, ct)
                ?? throw new NotFoundException($"Recomendação {id} não encontrada.");
        return Map(r);
    }

    public async Task<RecomendacaoResponse> AtualizarStatusAsync(int id, RecomendacaoStatusRequest req, CancellationToken ct = default)
    {
        var r = await _db.Recomendacoes.FirstOrDefaultAsync(x => x.RecId == id, ct)
                ?? throw new NotFoundException($"Recomendação {id} não encontrada.");
        r.RecStatus = req.Status;
        await _db.SaveChangesAsync(ct);
        return Map(r);
    }

    private static RecomendacaoResponse Map(Recomendacao r) => new()
    {
        Id = r.RecId,
        ResultadoId = r.RecRstId,
        Tipo = r.RecTipo,
        Descricao = r.RecDescricao,
        Status = r.RecStatus,
        DataGeracao = r.RecDtGeracao
    };
}
