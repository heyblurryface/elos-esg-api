namespace Kaizen.Api.Models;

/// <summary>Pilar de maturidade (Governança, Segurança da Informação, etc.).</summary>
public class Pilar
{
    public int PilId { get; set; }
    public string PilNome { get; set; } = string.Empty;
    public int PilPontuacaoMax { get; set; }

    public ICollection<Pergunta> Perguntas { get; set; } = new List<Pergunta>();
    public ICollection<Resultado> Resultados { get; set; } = new List<Resultado>();
}
