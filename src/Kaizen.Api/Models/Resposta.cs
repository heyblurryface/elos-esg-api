namespace Kaizen.Api.Models;

/// <summary>Resposta dada a uma pergunta dentro de uma avaliação (0, 1 ou 2).</summary>
public class Resposta
{
    public int ResId { get; set; }
    public int ResAvaId { get; set; }
    public int ResPergId { get; set; }
    public int ResValor { get; set; }
    public DateTime ResDtResposta { get; set; }

    public Avaliacao? Avaliacao { get; set; }
    public Pergunta? Pergunta { get; set; }
}
