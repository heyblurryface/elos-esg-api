namespace Kaizen.Api.Models;

/// <summary>Aplicação de um diagnóstico de uma empresa por um consultor.</summary>
public class Avaliacao
{
    public int AvaId { get; set; }
    public int AvaEmpId { get; set; }
    public int AvaConId { get; set; }
    public DateTime AvaDtAvaliacao { get; set; }
    public DateTime? AvaDtAtualizacao { get; set; }
    public string AvaStatus { get; set; } = "A"; // A = Aberta, E = Em andamento, F = Finalizada

    public Empresa? Empresa { get; set; }
    public Consultor? Consultor { get; set; }
    public ICollection<Resposta> Respostas { get; set; } = new List<Resposta>();
    public ICollection<Resultado> Resultados { get; set; } = new List<Resultado>();
}
