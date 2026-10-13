namespace Kaizen.Api.ViewModels.Resultados;

public class ResultadoResponse
{
    public int Id { get; set; }
    public int AvaliacaoId { get; set; }
    public int PilarId { get; set; }
    public string? PilarNome { get; set; }
    public int Pontuacao { get; set; }
    public int PontuacaoMaxima { get; set; }
    public string? NivelMaturidade { get; set; }
    public string? Prioridade { get; set; }
}
