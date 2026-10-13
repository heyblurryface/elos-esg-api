using System.ComponentModel.DataAnnotations;

namespace Kaizen.Api.ViewModels.Avaliacoes;

public class AvaliacaoRequest
{
    [Required, Range(1, int.MaxValue)]
    public int EmpresaId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int ConsultorId { get; set; }

    [Required]
    public DateTime DataAvaliacao { get; set; }

    [RegularExpression("^[AEF]$", ErrorMessage = "status deve ser 'A', 'E' ou 'F'.")]
    public string Status { get; set; } = "A";
}

public class AvaliacaoResponse
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string? EmpresaNome { get; set; }
    public int ConsultorId { get; set; }
    public string? ConsultorNome { get; set; }
    public DateTime DataAvaliacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public string Status { get; set; } = "A";
    public string StatusDescricao { get; set; } = string.Empty;
}
