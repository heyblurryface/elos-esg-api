using Kaizen.Api.Common.Pagination;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.ViewModels.Consultores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaizen.Api.Controllers;

[ApiController]
[Route("api/consultores")]
[Authorize]
public class ConsultoresController : ControllerBase
{
    private readonly IConsultorService _svc;
    public ConsultoresController(IConsultorService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ConsultorResponse>>> Listar([FromQuery] PageQuery page, CancellationToken ct)
        => Ok(await _svc.ListarAsync(page, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConsultorResponse>> Obter(int id, CancellationToken ct)
        => Ok(await _svc.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ConsultorResponse>> Criar([FromBody] ConsultorRequest req, CancellationToken ct)
    {
        var c = await _svc.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = c.Id }, c);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ConsultorResponse>> Atualizar(int id, [FromBody] ConsultorRequest req, CancellationToken ct)
        => Ok(await _svc.AtualizarAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await _svc.RemoverAsync(id, ct);
        return NoContent();
    }
}
