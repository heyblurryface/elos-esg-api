using System.ComponentModel.DataAnnotations;

namespace Kaizen.Api.ViewModels.Auth;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(255, MinimumLength = 6, ErrorMessage = "A senha deve ter ao menos 6 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Foto { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiraEm { get; set; }
    public int ConsultorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
