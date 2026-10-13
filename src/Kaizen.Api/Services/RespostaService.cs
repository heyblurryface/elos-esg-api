using Kaizen.Api.Common.Exceptions;
using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Respostas;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Services;

public class RespostaService : IRespostaService
{
    private readonly KaizenDbContext _db;
    private readonly IDiagnosticoEngine _engine;

    public RespostaService(KaizenDbContext db, IDiagnosticoEngine engine)
    {
        _db = db; _engine = engine;
    }

    public async Task<PagedResult<RespostaResponse>> ListarAsync(PageQuery page, int? avaliacaoId, CancellationToken ct = default)
    {
        var query = _db.Respostas.AsNoTracking().AsQueryable();
        if (avaliacaoId is not null) query = query.Where(r => r.ResAvaId == avaliacaoId);
        var ordered = query.OrderBy(r => r.ResId);
        var paged = await PagedResult<Resposta>.CreateAsync(ordered, page.Page, page.PageSize, ct);
        return paged.Map(Map);
    }

    public async Task<RespostaResponse> ObterAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.Respostas.AsNoTracking().FirstOrDefaultAsync(x => x.ResId == id, ct)
                ?? throw new NotFoundException($"Resposta {id} não encontrada.");
        return Map(r);
    }

    public async Task<RespostaResponse> CriarAsync(RespostaRequest req, CancellationToken ct = default)
    {
        await ValidarRefs(req, ct);

        if (await _db.Respostas.AnyAsync(r => r.ResAvaId == req.AvaliacaoId && r.ResPergId == req.PerguntaId, ct))
            throw new ConflictException("Já existe resposta para esta pergunta nesta avaliação. Use PUT para alterá-la.");

        var r = new Resposta
        {
            ResId = await NextIdAsync(ct),
            ResAvaId = req.AvaliacaoId,
            ResPergId = req.PerguntaId,
            ResValor = req.Valor,
            ResDtResposta = DateTime.UtcNow
        };
        _db.Respostas.Add(r);
        await _db.SaveChangesAsync(ct);

        // Dispara a engine (equivalente às triggers Oracle).
        await _engine.RecalcularAposRespostaAsync(req.AvaliacaoId, req.PerguntaId, ct);
        await _engine.AtualizarStatusAvaliacaoAsync(req.AvaliacaoId, ct);

        return Map(r);
    }

    public async Task<RespostaResponse> AtualizarAsync(int id, RespostaRequest req, CancellationToken ct = default)
    {
        var r = await _db.Respostas.FirstOrDefaultAsync(x => x.ResId == id, ct)
                ?? throw new NotFoundException($"Resposta {id} não encontrada.");
        await ValidarRefs(req, ct);

        if (await _db.Respostas.AnyAsync(x =>
                x.ResAvaId == req.AvaliacaoId && x.ResPergId == req.PerguntaId && x.ResId != id, ct))
            throw new ConflictException("Já existe outra resposta para esta pergunta nesta avaliação.");

        r.ResAvaId = req.AvaliacaoId;
        r.ResPergId = req.PerguntaId;
        r.ResValor = req.Valor;
        r.ResDtResposta = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _engine.RecalcularAposRespostaAsync(req.AvaliacaoId, req.PerguntaId, ct);
        await _engine.AtualizarStatusAvaliacaoAsync(req.AvaliacaoId, ct);

        return Map(r);
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.Respostas.FirstOrDefaultAsync(x => x.ResId == id, ct)
                ?? throw new NotFoundException($"Resposta {id} não encontrada.");
        var (avaId, pergId) = (r.ResAvaId, r.ResPergId);

        _db.Respostas.Remove(r);
        await _db.SaveChangesAsync(ct);

        await _engine.RecalcularAposRespostaAsync(avaId, pergId, ct);
        await _engine.AtualizarStatusAvaliacaoAsync(avaId, ct);
    }

    private async Task ValidarRefs(RespostaRequest req, CancellationToken ct)
    {
        if (!await _db.Avaliacoes.AnyAsync(a => a.AvaId == req.AvaliacaoId, ct))
            throw new BusinessRuleException($"Avaliação {req.AvaliacaoId} não existe.");
        if (!await _db.Perguntas.AnyAsync(p => p.PergId == req.PerguntaId, ct))
            throw new BusinessRuleException($"Pergunta {req.PerguntaId} não existe.");
    }

    private async Task<int> NextIdAsync(CancellationToken ct) =>
        await _db.Respostas.AnyAsync(ct) ? await _db.Respostas.MaxAsync(r => r.ResId, ct) + 1 : 1;

    private static RespostaResponse Map(Resposta r) => new()
    {
        Id = r.ResId,
        AvaliacaoId = r.ResAvaId,
        PerguntaId = r.ResPergId,
        Valor = r.ResValor,
        DataResposta = r.ResDtResposta
    };
}
