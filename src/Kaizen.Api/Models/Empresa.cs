namespace Kaizen.Api.Models;

/// <summary>Empresa avaliada no diagnóstico ESG / GRC.</summary>
public class Empresa
{
    public int EmpId { get; set; }
    public string EmpNome { get; set; } = string.Empty;
    public string? EmpSetor { get; set; }
    public string? EmpPorte { get; set; }
    public int? EmpNumFunc { get; set; }
    public string? EmpResp { get; set; }
    public string? EmpRespCargo { get; set; }
    public string? EmpRespAreas { get; set; }
    public string EmpGestTerc { get; set; } = "I"; // S / N / I
    public string? EmpFoto { get; set; }
    public DateTime EmpDtCadastro { get; set; }

    public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
}
