using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Recomendacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaizen.Api.Controllers;

[ApiController]
[Route("api/recomendacoes")]
[Authorize]
public class RecomendacoesController : ControllerBase
{
    private readonly IRecomendacaoService _svc;
    public RecomendacoesController(IRecomendacaoService svc) => _svc = svc;

    /// <summary>Recomendações geradas pela engine. Filtro opcional por resultado.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<RecomendacaoResponse>>> Listar(
        [FromQuery] PageQuery page, [FromQuery] int? resultadoId, CancellationToken ct)
        => Ok(await _svc.ListarAsync(page, resultadoId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecomendacaoResponse>> Obter(int id, CancellationToken ct)
        => Ok(await _svc.ObterAsync(id, ct));

    /// <summary>Atualiza apenas o status da recomendação (ex.: Pendente -> Concluido).</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<RecomendacaoResponse>> AtualizarStatus(
        int id, [FromBody] RecomendacaoStatusRequest req, CancellationToken ct)
        => Ok(await _svc.AtualizarStatusAsync(id, req, ct));
}
