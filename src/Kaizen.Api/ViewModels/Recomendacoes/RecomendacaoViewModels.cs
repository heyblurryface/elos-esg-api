using System.ComponentModel.DataAnnotations;

namespace Kaizen.Api.ViewModels.Recomendacoes;

public class RecomendacaoResponse
{
    public int Id { get; set; }
    public int ResultadoId { get; set; }
    public string? Tipo { get; set; }
    public string? Descricao { get; set; }
    public string Status { get; set; } = "Pendente";
    public DateTime DataGeracao { get; set; }
}

public class RecomendacaoStatusRequest
{
    [Required]
    [RegularExpression("^(Pendente|Em andamento|Concluido)$",
        ErrorMessage = "status deve ser 'Pendente', 'Em andamento' ou 'Concluido'.")]
    public string Status { get; set; } = "Pendente";
}
