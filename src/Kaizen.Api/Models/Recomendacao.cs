namespace Kaizen.Api.Models;

/// <summary>Recomendação gerada automaticamente a partir do resultado de um pilar.</summary>
public class Recomendacao
{
    public int RecId { get; set; }
    public int RecRstId { get; set; }
    public string? RecTipo { get; set; }
    public string? RecDescricao { get; set; }
    public string RecStatus { get; set; } = "Pendente";
    public DateTime RecDtGeracao { get; set; }

    public Resultado? Resultado { get; set; }
}
