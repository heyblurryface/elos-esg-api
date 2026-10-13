namespace Kaizen.Api.Models;

/// <summary>Resultado consolidado de um pilar dentro de uma avaliação (gerado pela engine).</summary>
public class Resultado
{
    public int RstId { get; set; }
    public int RstAvaId { get; set; }
    public int RstPilId { get; set; }
    public int RstPontuacao { get; set; }
    public string? RstNivelMaturidade { get; set; }
    public string? RstPrioridade { get; set; }

    public Avaliacao? Avaliacao { get; set; }
    public Pilar? Pilar { get; set; }
    public ICollection<Recomendacao> Recomendacoes { get; set; } = new List<Recomendacao>();
}
