using Kaizen.Api.Data;
using Kaizen.Api.Models;
using Kaizen.Api.Services.Diagnostico;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kaizen.Api.Tests.Services;

public class DiagnosticoEngineTests
{
    private static KaizenDbContext NovoContexto()
    {
        var options = new DbContextOptionsBuilder<KaizenDbContext>()
            .UseInMemoryDatabase("EngineTest_" + Guid.NewGuid())
            .Options;
        return new KaizenDbContext(options);
    }

    /// <summary>Cenário base: 1 pilar (max 12), 2 perguntas peso 2, 1 avaliação.</summary>
    private static async Task<KaizenDbContext> SeedBaseAsync()
    {
        var db = NovoContexto();
        db.Pilares.Add(new Pilar { PilId = 1, PilNome = "Governança", PilPontuacaoMax = 12 });
        db.Perguntas.Add(new Pergunta { PergId = 1, PergPilId = 1, PergDescricao = "P1", PergPeso = 2 });
        db.Perguntas.Add(new Pergunta { PergId = 2, PergPilId = 1, PergDescricao = "P2", PergPeso = 2 });
        db.Empresas.Add(new Empresa { EmpId = 100, EmpNome = "Empresa Teste", EmpGestTerc = "I", EmpDtCadastro = DateTime.UtcNow });
        db.Consultores.Add(new Consultor { ConId = 200, ConNome = "Consultor", ConEmail = "c@k.com", ConSenha = "x", ConDtCadastro = DateTime.UtcNow });
        db.Avaliacoes.Add(new Avaliacao { AvaId = 10, AvaEmpId = 100, AvaConId = 200, AvaDtAvaliacao = DateTime.UtcNow, AvaStatus = "A" });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Recalcular_ComScorePositivo_CriaResultadoERecomendacao()
    {
        var db = await SeedBaseAsync();
        db.Respostas.Add(new Resposta { ResId = 1, ResAvaId = 10, ResPergId = 1, ResValor = 2, ResDtResposta = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var engine = new DiagnosticoEngine(db);
        await engine.RecalcularAposRespostaAsync(10, 1);

        var resultado = await db.Resultados.SingleAsync(r => r.RstAvaId == 10 && r.RstPilId == 1);
        Assert.Equal(4, resultado.RstPontuacao); // valor 2 * peso 2
        Assert.False(string.IsNullOrWhiteSpace(resultado.RstNivelMaturidade));

        var recomendacao = await db.Recomendacoes.SingleAsync(r => r.RecRstId == resultado.RstId);
        Assert.Equal("Pendente", recomendacao.RecStatus);
        Assert.False(string.IsNullOrWhiteSpace(recomendacao.RecTipo));
    }

    [Fact]
    public async Task Recalcular_QuandoScoreZera_RemoveResultadoERecomendacao()
    {
        var db = await SeedBaseAsync();
        var resposta = new Resposta { ResId = 1, ResAvaId = 10, ResPergId = 1, ResValor = 2, ResDtResposta = DateTime.UtcNow };
        db.Respostas.Add(resposta);
        await db.SaveChangesAsync();

        var engine = new DiagnosticoEngine(db);
        await engine.RecalcularAposRespostaAsync(10, 1);
        Assert.True(await db.Resultados.AnyAsync());

        // Zera a resposta -> score 0 -> resultado e recomendação devem ser removidos.
        resposta.ResValor = 0;
        await db.SaveChangesAsync();
        await engine.RecalcularAposRespostaAsync(10, 1);

        Assert.False(await db.Resultados.AnyAsync());
        Assert.False(await db.Recomendacoes.AnyAsync());
    }

    [Fact]
    public async Task AtualizarStatus_ComTodasPerguntasRespondidas_FinalizaAvaliacao()
    {
        var db = await SeedBaseAsync();
        db.Respostas.Add(new Resposta { ResId = 1, ResAvaId = 10, ResPergId = 1, ResValor = 2, ResDtResposta = DateTime.UtcNow });
        db.Respostas.Add(new Resposta { ResId = 2, ResAvaId = 10, ResPergId = 2, ResValor = 1, ResDtResposta = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var engine = new DiagnosticoEngine(db);
        await engine.AtualizarStatusAvaliacaoAsync(10);

        var avaliacao = await db.Avaliacoes.SingleAsync(a => a.AvaId == 10);
        Assert.Equal("F", avaliacao.AvaStatus); // 2 de 2 perguntas respondidas
    }

    [Fact]
    public async Task AtualizarStatus_ComRespostasParciais_MarcaEmAndamento()
    {
        var db = await SeedBaseAsync();
        db.Respostas.Add(new Resposta { ResId = 1, ResAvaId = 10, ResPergId = 1, ResValor = 2, ResDtResposta = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var engine = new DiagnosticoEngine(db);
        await engine.AtualizarStatusAvaliacaoAsync(10);

        var avaliacao = await db.Avaliacoes.SingleAsync(a => a.AvaId == 10);
        Assert.Equal("E", avaliacao.AvaStatus); // 1 de 2 perguntas respondidas
    }
}
