using Kaizen.Api.Common.Pagination;
using Kaizen.Api.ViewModels.Auth;
using Kaizen.Api.ViewModels.Avaliacoes;
using Kaizen.Api.ViewModels.Consultores;
using Kaizen.Api.ViewModels.Empresas;
using Kaizen.Api.ViewModels.Recomendacoes;
using Kaizen.Api.ViewModels.Respostas;
using Kaizen.Api.ViewModels.Resultados;

namespace Kaizen.Api.Services.Abstractions;

public interface IJwtService
{
    (string token, DateTime expiraEm) GerarToken(int consultorId, string nome, string email);
}

public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verify(string senha, string hash);
}

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
}

public interface IConsultorService
{
    Task<PagedResult<ConsultorResponse>> ListarAsync(PageQuery page, CancellationToken ct = default);
    Task<ConsultorResponse> ObterAsync(int id, CancellationToken ct = default);
    Task<ConsultorResponse> CriarAsync(ConsultorRequest req, CancellationToken ct = default);
    Task<ConsultorResponse> AtualizarAsync(int id, ConsultorRequest req, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IEmpresaService
{
    Task<PagedResult<EmpresaResponse>> ListarAsync(PageQuery page, string? setor, CancellationToken ct = default);
    Task<EmpresaResponse> ObterAsync(int id, CancellationToken ct = default);
    Task<EmpresaResponse> CriarAsync(EmpresaRequest req, CancellationToken ct = default);
    Task<EmpresaResponse> AtualizarAsync(int id, EmpresaRequest req, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IAvaliacaoService
{
    Task<PagedResult<AvaliacaoResponse>> ListarAsync(PageQuery page, int? empresaId, string? status, CancellationToken ct = default);
    Task<AvaliacaoResponse> ObterAsync(int id, CancellationToken ct = default);
    Task<AvaliacaoResponse> CriarAsync(AvaliacaoRequest req, CancellationToken ct = default);
    Task<AvaliacaoResponse> AtualizarAsync(int id, AvaliacaoRequest req, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IRespostaService
{
    Task<PagedResult<RespostaResponse>> ListarAsync(PageQuery page, int? avaliacaoId, CancellationToken ct = default);
    Task<RespostaResponse> ObterAsync(int id, CancellationToken ct = default);
    Task<RespostaResponse> CriarAsync(RespostaRequest req, CancellationToken ct = default);
    Task<RespostaResponse> AtualizarAsync(int id, RespostaRequest req, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IResultadoService
{
    Task<PagedResult<ResultadoResponse>> ListarAsync(PageQuery page, int? avaliacaoId, CancellationToken ct = default);
    Task<ResultadoResponse> ObterAsync(int id, CancellationToken ct = default);
}

public interface IRecomendacaoService
{
    Task<PagedResult<RecomendacaoResponse>> ListarAsync(PageQuery page, int? resultadoId, CancellationToken ct = default);
    Task<RecomendacaoResponse> ObterAsync(int id, CancellationToken ct = default);
    Task<RecomendacaoResponse> AtualizarStatusAsync(int id, RecomendacaoStatusRequest req, CancellationToken ct = default);
}

/// <summary>
/// Engine de diagnóstico: reimplementa em C# a lógica que no Oracle ficava em triggers
/// (cálculo de score por pilar, classificação de maturidade, geração de recomendação e
/// transição de status da avaliação). Centraliza a regra para ficar testável por unidade.
/// </summary>
public interface IDiagnosticoEngine
{
    Task RecalcularAposRespostaAsync(int avaliacaoId, int perguntaId, CancellationToken ct = default);
    Task AtualizarStatusAvaliacaoAsync(int avaliacaoId, CancellationToken ct = default);
}
