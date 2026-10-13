using System.ComponentModel.DataAnnotations;

namespace Kaizen.Api.ViewModels.Respostas;

public class RespostaRequest
{
    [Required, Range(1, int.MaxValue)]
    public int AvaliacaoId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int PerguntaId { get; set; }

    [Required, Range(0, 2, ErrorMessage = "valor deve ser 0, 1 ou 2.")]
    public int Valor { get; set; }
}

public class RespostaResponse
{
    public int Id { get; set; }
    public int AvaliacaoId { get; set; }
    public int PerguntaId { get; set; }
    public int Valor { get; set; }
    public DateTime DataResposta { get; set; }
}
