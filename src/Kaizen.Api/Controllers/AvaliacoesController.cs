using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Avaliacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaizen.Api.Controllers;

[ApiController]
[Route("api/avaliacoes")]
[Authorize]
public class AvaliacoesController : ControllerBase
{
    private readonly IAvaliacaoService _svc;
    public AvaliacoesController(IAvaliacaoService svc) => _svc = svc;

    /// <summary>Lista avaliações com paginação e filtros opcionais (empresaId, status).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AvaliacaoResponse>>> Listar(
        [FromQuery] PageQuery page, [FromQuery] int? empresaId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await _svc.ListarAsync(page, empresaId, status, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AvaliacaoResponse>> Obter(int id, CancellationToken ct)
        => Ok(await _svc.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<AvaliacaoResponse>> Criar([FromBody] AvaliacaoRequest req, CancellationToken ct)
    {
        var a = await _svc.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = a.Id }, a);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AvaliacaoResponse>> Atualizar(int id, [FromBody] AvaliacaoRequest req, CancellationToken ct)
        => Ok(await _svc.AtualizarAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await _svc.RemoverAsync(id, ct);
        return NoContent();
    }
}
