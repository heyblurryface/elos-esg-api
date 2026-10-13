namespace Kaizen.Api.Models;

/// <summary>Consultor responsável por aplicar os diagnósticos (também é o usuário autenticável).</summary>
public class Consultor
{
    public int ConId { get; set; }
    public string ConNome { get; set; } = string.Empty;
    public string ConEmail { get; set; } = string.Empty;
    public string ConSenha { get; set; } = string.Empty; // hash BCrypt
    public string? ConFoto { get; set; }
    public DateTime ConDtCadastro { get; set; }

    public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
}
