using System.ComponentModel.DataAnnotations;

namespace Kaizen.Api.ViewModels.Consultores;

public class ConsultorRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    // Em update, senha nula/vazia mantém a atual.
    [StringLength(255, MinimumLength = 6)]
    public string? Senha { get; set; }

    [StringLength(255)]
    public string? Foto { get; set; }
}

public class ConsultorResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Foto { get; set; }
    public DateTime DataCadastro { get; set; }
}
