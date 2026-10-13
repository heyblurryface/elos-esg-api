namespace Kaizen.Api.Models;

/// <summary>Pergunta do questionário, vinculada a um pilar e com peso próprio.</summary>
public class Pergunta
{
    public int PergId { get; set; }
    public int PergPilId { get; set; }
    public string PergDescricao { get; set; } = string.Empty;
    public int PergPeso { get; set; } = 2;

    public Pilar? Pilar { get; set; }
    public ICollection<Resposta> Respostas { get; set; } = new List<Resposta>();
}
