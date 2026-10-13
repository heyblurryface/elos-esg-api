using Kaizen.Api.Common.Exceptions;
using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Avaliacoes;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Services;

public class AvaliacaoService : IAvaliacaoService
{
    private readonly KaizenDbContext _db;
    public AvaliacaoService(KaizenDbContext db) => _db = db;

    public async Task<PagedResult<AvaliacaoResponse>> ListarAsync(PageQuery page, int? empresaId, string? status, CancellationToken ct = default)
    {
        var query = _db.Avaliacoes.AsNoTracking()
            .Include(a => a.Empresa).Include(a => a.Consultor).AsQueryable();

        if (empresaId is not null) query = query.Where(a => a.AvaEmpId == empresaId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.AvaStatus == status.ToUpper());

        var ordered = query.OrderByDescending(a => a.AvaDtAvaliacao).ThenBy(a => a.AvaId);
        var paged = await PagedResult<Avaliacao>.CreateAsync(ordered, page.Page, page.PageSize, ct);
        return paged.Map(Map);
    }

    public async Task<AvaliacaoResponse> ObterAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.Avaliacoes.AsNoTracking()
                    .Include(x => x.Empresa).Include(x => x.Consultor)
                    .FirstOrDefaultAsync(x => x.AvaId == id, ct)
                ?? throw new NotFoundException($"Avaliação {id} não encontrada.");
        return Map(a);
    }

    public async Task<AvaliacaoResponse> CriarAsync(AvaliacaoRequest req, CancellationToken ct = default)
    {
        if (!await _db.Empresas.AnyAsync(e => e.EmpId == req.EmpresaId, ct))
            throw new BusinessRuleException($"Empresa {req.EmpresaId} não existe.");
        if (!await _db.Consultores.AnyAsync(c => c.ConId == req.ConsultorId, ct))
            throw new BusinessRuleException($"Consultor {req.ConsultorId} não existe.");

        var a = new Avaliacao
        {
            AvaId = await NextIdAsync(ct),
            AvaEmpId = req.EmpresaId,
            AvaConId = req.ConsultorId,
            AvaDtAvaliacao = req.DataAvaliacao,
            AvaDtAtualizacao = req.DataAvaliacao,
            AvaStatus = string.IsNullOrWhiteSpace(req.Status) ? "A" : req.Status.ToUpper()
        };
        _db.Avaliacoes.Add(a);
        await _db.SaveChangesAsync(ct);
        return await ObterAsync(a.AvaId, ct);
    }

    public async Task<AvaliacaoResponse> AtualizarAsync(int id, AvaliacaoRequest req, CancellationToken ct = default)
    {
        var a = await _db.Avaliacoes.FirstOrDefaultAsync(x => x.AvaId == id, ct)
                ?? throw new NotFoundException($"Avaliação {id} não encontrada.");

        if (!await _db.Empresas.AnyAsync(e => e.EmpId == req.EmpresaId, ct))
            throw new BusinessRuleException($"Empresa {req.EmpresaId} não existe.");
        if (!await _db.Consultores.AnyAsync(c => c.ConId == req.ConsultorId, ct))
            throw new BusinessRuleException($"Consultor {req.ConsultorId} não existe.");

        a.AvaEmpId = req.EmpresaId;
        a.AvaConId = req.ConsultorId;
        a.AvaDtAvaliacao = req.DataAvaliacao;
        a.AvaStatus = string.IsNullOrWhiteSpace(req.Status) ? a.AvaStatus : req.Status.ToUpper();
        a.AvaDtAtualizacao = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await ObterAsync(a.AvaId, ct);
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.Avaliacoes.FirstOrDefaultAsync(x => x.AvaId == id, ct)
                ?? throw new NotFoundException($"Avaliação {id} não encontrada.");

        // Remove dependências em cascata manual (respostas, resultados, recomendações).
        var resultadoIds = await _db.Resultados.Where(r => r.RstAvaId == id).Select(r => r.RstId).ToListAsync(ct);
        _db.Recomendacoes.RemoveRange(_db.Recomendacoes.Where(r => resultadoIds.Contains(r.RecRstId)));
        _db.Resultados.RemoveRange(_db.Resultados.Where(r => r.RstAvaId == id));
        _db.Respostas.RemoveRange(_db.Respostas.Where(r => r.ResAvaId == id));
        _db.Avaliacoes.Remove(a);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> NextIdAsync(CancellationToken ct) =>
        await _db.Avaliacoes.AnyAsync(ct) ? await _db.Avaliacoes.MaxAsync(a => a.AvaId, ct) + 1 : 1;

    private static string Descricao(string status) => status switch
    {
        "A" => "Aberta",
        "E" => "Em andamento",
        "F" => "Finalizada",
        _ => "Desconhecido"
    };

    private static AvaliacaoResponse Map(Avaliacao a) => new()
    {
        Id = a.AvaId,
        EmpresaId = a.AvaEmpId,
        EmpresaNome = a.Empresa?.EmpNome,
        ConsultorId = a.AvaConId,
        ConsultorNome = a.Consultor?.ConNome,
        DataAvaliacao = a.AvaDtAvaliacao,
        DataAtualizacao = a.AvaDtAtualizacao,
        Status = a.AvaStatus,
        StatusDescricao = Descricao(a.AvaStatus)
    };
}
