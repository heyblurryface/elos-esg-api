using Kaizen.Api.Common.Exceptions;
using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Resultados;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Services;

public class ResultadoService : IResultadoService
{
    private readonly KaizenDbContext _db;
    public ResultadoService(KaizenDbContext db) => _db = db;

    public async Task<PagedResult<ResultadoResponse>> ListarAsync(PageQuery page, int? avaliacaoId, CancellationToken ct = default)
    {
        var query = _db.Resultados.AsNoTracking().Include(r => r.Pilar).AsQueryable();
        if (avaliacaoId is not null) query = query.Where(r => r.RstAvaId == avaliacaoId);
        var ordered = query.OrderBy(r => r.RstAvaId).ThenBy(r => r.RstPilId);
        var paged = await PagedResult<Resultado>.CreateAsync(ordered, page.Page, page.PageSize, ct);
        return paged.Map(Map);
    }

    public async Task<ResultadoResponse> ObterAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.Resultados.AsNoTracking().Include(x => x.Pilar)
                    .FirstOrDefaultAsync(x => x.RstId == id, ct)
                ?? throw new NotFoundException($"Resultado {id} não encontrado.");
        return Map(r);
    }

    private static ResultadoResponse Map(Resultado r) => new()
    {
        Id = r.RstId,
        AvaliacaoId = r.RstAvaId,
        PilarId = r.RstPilId,
        PilarNome = r.Pilar?.PilNome,
        Pontuacao = r.RstPontuacao,
        PontuacaoMaxima = r.Pilar?.PilPontuacaoMax ?? 0,
        NivelMaturidade = r.RstNivelMaturidade,
        Prioridade = r.RstPrioridade
    };
}
