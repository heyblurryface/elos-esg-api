using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Services.Diagnostico;

/// <summary>
/// Substitui as triggers Oracle (TRG_CALCULA_SCORE, TRG_RESULTADO_PILAR,
/// TRG_CLASSIFICA_MATURIDADE, TRG_GERA_RECOMENDACAO) por lógica de aplicação em C#.
/// É chamada pelo RespostaService sempre que uma resposta é criada, alterada ou removida.
/// </summary>
public class DiagnosticoEngine : IDiagnosticoEngine
{
    private readonly KaizenDbContext _db;
    public DiagnosticoEngine(KaizenDbContext db) => _db = db;

    public async Task RecalcularAposRespostaAsync(int avaliacaoId, int perguntaId, CancellationToken ct = default)
    {
        var pilarId = await _db.Perguntas
            .Where(p => p.PergId == perguntaId)
            .Select(p => (int?)p.PergPilId)
            .FirstOrDefaultAsync(ct);

        if (pilarId is null) return;

        // Score do pilar = SUM(valor * peso) das respostas da avaliação naquele pilar.
        var score = await _db.Respostas
            .Where(r => r.ResAvaId == avaliacaoId)
            .Join(_db.Perguntas, r => r.ResPergId, p => p.PergId, (r, p) => new { r, p })
            .Where(x => x.p.PergPilId == pilarId.Value)
            .SumAsync(x => (int?)(x.r.ResValor * x.p.PergPeso), ct) ?? 0;

        var resultado = await _db.Resultados
            .FirstOrDefaultAsync(r => r.RstAvaId == avaliacaoId && r.RstPilId == pilarId.Value, ct);

        if (score > 0)
        {
            var pontuacaoMax = await _db.Pilares
                .Where(p => p.PilId == pilarId.Value)
                .Select(p => p.PilPontuacaoMax)
                .FirstOrDefaultAsync(ct);

            var classe = ClassificadorMaturidade.Classificar(score, pontuacaoMax);

            if (resultado is null)
            {
                resultado = new Resultado
                {
                    RstId = await NextResultadoIdAsync(ct),
                    RstAvaId = avaliacaoId,
                    RstPilId = pilarId.Value,
                    RstPontuacao = score,
                    RstNivelMaturidade = classe.NivelMaturidade,
                    RstPrioridade = classe.Prioridade
                };
                _db.Resultados.Add(resultado);
            }
            else
            {
                resultado.RstPontuacao = score;
                resultado.RstNivelMaturidade = classe.NivelMaturidade;
                resultado.RstPrioridade = classe.Prioridade;
            }

            await _db.SaveChangesAsync(ct);
            await UpsertRecomendacaoAsync(resultado, ct);
        }
        else if (resultado is not null)
        {
            // Score zerou (todas as respostas removidas/0): remove resultado e recomendações.
            var recs = _db.Recomendacoes.Where(rec => rec.RecRstId == resultado.RstId);
            _db.Recomendacoes.RemoveRange(recs);
            _db.Resultados.Remove(resultado);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AtualizarStatusAvaliacaoAsync(int avaliacaoId, CancellationToken ct = default)
    {
        var avaliacao = await _db.Avaliacoes.FirstOrDefaultAsync(a => a.AvaId == avaliacaoId, ct);
        if (avaliacao is null) return;

        var totalPerguntas = await _db.Perguntas.CountAsync(ct);
        var respondidas = await _db.Respostas
            .Where(r => r.ResAvaId == avaliacaoId)
            .Select(r => r.ResPergId)
            .Distinct()
            .CountAsync(ct);

        if (totalPerguntas > 0 && respondidas >= totalPerguntas)
            avaliacao.AvaStatus = "F";                 // Finalizada
        else if (avaliacao.AvaStatus != "F")
            avaliacao.AvaStatus = "E";                 // Em andamento

        avaliacao.AvaDtAtualizacao = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertRecomendacaoAsync(Resultado resultado, CancellationToken ct)
    {
        var (tipo, descricao) = ClassificadorMaturidade.GerarRecomendacao(resultado.RstNivelMaturidade);

        var rec = await _db.Recomendacoes.FirstOrDefaultAsync(r => r.RecRstId == resultado.RstId, ct);
        if (rec is null)
        {
            rec = new Recomendacao
            {
                RecId = await NextRecomendacaoIdAsync(ct),
                RecRstId = resultado.RstId,
                RecTipo = tipo,
                RecDescricao = descricao,
                RecStatus = "Pendente",
                RecDtGeracao = DateTime.UtcNow
            };
            _db.Recomendacoes.Add(rec);
        }
        else
        {
            rec.RecTipo = tipo;
            rec.RecDescricao = descricao;
            rec.RecStatus = "Pendente";
            rec.RecDtGeracao = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> NextResultadoIdAsync(CancellationToken ct) =>
        await _db.Resultados.AnyAsync(ct) ? await _db.Resultados.MaxAsync(r => r.RstId, ct) + 1 : 1;

    private async Task<int> NextRecomendacaoIdAsync(CancellationToken ct) =>
        await _db.Recomendacoes.AnyAsync(ct) ? await _db.Recomendacoes.MaxAsync(r => r.RecId, ct) + 1 : 1;
}
