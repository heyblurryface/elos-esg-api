using Kaizen.Api.Services.Diagnostico;
using Xunit;

namespace Kaizen.Api.Tests.Services;

public class ClassificadorMaturidadeTests
{
    [Theory]
    [InlineData(0, 12, "Inicial", "Alta")]            // 0%   -> Inicial
    [InlineData(3, 12, "Inicial", "Alta")]            // 25%  -> Inicial (limite)
    [InlineData(4, 12, "Em desenvolvimento", "Alta")] // 33%  -> Em desenvolvimento
    [InlineData(6, 12, "Em desenvolvimento", "Alta")] // 50%  -> Em desenvolvimento (limite)
    [InlineData(7, 12, "Estruturado", "Media")]       // 58%  -> Estruturado
    [InlineData(9, 12, "Estruturado", "Media")]       // 75%  -> Estruturado (limite)
    [InlineData(10, 12, "Avancado", "Manter")]        // 83%  -> Avancado
    [InlineData(12, 12, "Avancado", "Manter")]        // 100% -> Avancado
    public void Classificar_RetornaNivelEPrioridadeEsperados(
        int pontuacao, int maximo, string nivelEsperado, string prioridadeEsperada)
    {
        var resultado = ClassificadorMaturidade.Classificar(pontuacao, maximo);

        Assert.Equal(nivelEsperado, resultado.NivelMaturidade);
        Assert.Equal(prioridadeEsperada, resultado.Prioridade);
    }

    [Fact]
    public void Classificar_ComMaximoZero_NaoLancaERetornaInicial()
    {
        var resultado = ClassificadorMaturidade.Classificar(0, 0);
        Assert.Equal("Inicial", resultado.NivelMaturidade);
    }

    [Theory]
    [InlineData("Inicial", "Urgente")]
    [InlineData("Em desenvolvimento", "Urgente")]
    [InlineData("Estruturado", "Melhoria")]
    [InlineData("Avancado", "Manter")]
    [InlineData(null, "Indefinido")]
    public void GerarRecomendacao_MapeiaTipoPorNivel(string? nivel, string tipoEsperado)
    {
        var (tipo, descricao) = ClassificadorMaturidade.GerarRecomendacao(nivel);

        Assert.Equal(tipoEsperado, tipo);
        Assert.False(string.IsNullOrWhiteSpace(descricao));
    }
}
