using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaizen.Api.Controllers;

[ApiController]
[Route("api/resultados")]
[Authorize]
public class ResultadosController : ControllerBase
{
    private readonly IResultadoService _svc;
    public ResultadosController(IResultadoService svc) => _svc = svc;

    /// <summary>Score por pilar calculado pela engine. Filtro opcional por avaliação.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ResultadoResponse>>> Listar(
        [FromQuery] PageQuery page, [FromQuery] int? avaliacaoId, CancellationToken ct)
        => Ok(await _svc.ListarAsync(page, avaliacaoId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResultadoResponse>> Obter(int id, CancellationToken ct)
        => Ok(await _svc.ObterAsync(id, ct));
}
