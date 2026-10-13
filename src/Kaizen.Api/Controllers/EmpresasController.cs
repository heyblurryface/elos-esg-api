using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Empresas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaizen.Api.Controllers;

[ApiController]
[Route("api/empresas")]
[Authorize]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaService _svc;
    public EmpresasController(IEmpresaService svc) => _svc = svc;

    /// <summary>Lista empresas com paginação (?page=1&amp;pageSize=10) e filtro opcional por setor.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<EmpresaResponse>>> Listar(
        [FromQuery] PageQuery page, [FromQuery] string? setor, CancellationToken ct)
        => Ok(await _svc.ListarAsync(page, setor, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpresaResponse>> Obter(int id, CancellationToken ct)
        => Ok(await _svc.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<EmpresaResponse>> Criar([FromBody] EmpresaRequest req, CancellationToken ct)
    {
        var e = await _svc.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = e.Id }, e);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmpresaResponse>> Atualizar(int id, [FromBody] EmpresaRequest req, CancellationToken ct)
        => Ok(await _svc.AtualizarAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await _svc.RemoverAsync(id, ct);
        return NoContent();
    }
}
