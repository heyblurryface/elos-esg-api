using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Respostas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaizen.Api.Controllers;

/// <summary>
/// Endpoint central do diagnóstico: cada resposta criada/alterada/removida dispara a engine
/// que recalcula o resultado do pilar, reclassifica a maturidade e regenera a recomendação.
/// </summary>
[ApiController]
[Route("api/respostas")]
[Authorize]
public class RespostasController : ControllerBase
{
    private readonly IRespostaService _svc;
    public RespostasController(IRespostaService svc) => _svc = svc;

    /// <summary>Lista respostas com paginação e filtro opcional por avaliação.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<RespostaResponse>>> Listar(
        [FromQuery] PageQuery page, [FromQuery] int? avaliacaoId, CancellationToken ct)
        => Ok(await _svc.ListarAsync(page, avaliacaoId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RespostaResponse>> Obter(int id, CancellationToken ct)
        => Ok(await _svc.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<RespostaResponse>> Criar([FromBody] RespostaRequest req, CancellationToken ct)
    {
        var r = await _svc.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RespostaResponse>> Atualizar(int id, [FromBody] RespostaRequest req, CancellationToken ct)
        => Ok(await _svc.AtualizarAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await _svc.RemoverAsync(id, ct);
        return NoContent();
    }
}
