using System.ComponentModel.DataAnnotations;

namespace Kaizen.Api.ViewModels.Empresas;

public class EmpresaRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Setor { get; set; }

    [StringLength(20)]
    public string? Porte { get; set; }

    [Range(0, int.MaxValue)]
    public int? NumeroFuncionarios { get; set; }

    [StringLength(100)]
    public string? Responsavel { get; set; }

    [StringLength(100)]
    public string? ResponsavelCargo { get; set; }

    [StringLength(255)]
    public string? ResponsavelAreas { get; set; }

    [RegularExpression("^[SNI]$", ErrorMessage = "gestaoTerceiros deve ser 'S', 'N' ou 'I'.")]
    public string GestaoTerceiros { get; set; } = "I";

    [StringLength(255)]
    public string? Foto { get; set; }
}

public class EmpresaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Setor { get; set; }
    public string? Porte { get; set; }
    public int? NumeroFuncionarios { get; set; }
    public string? Responsavel { get; set; }
    public string? ResponsavelCargo { get; set; }
    public string? ResponsavelAreas { get; set; }
    public string GestaoTerceiros { get; set; } = "I";
    public string? Foto { get; set; }
    public DateTime DataCadastro { get; set; }
}
