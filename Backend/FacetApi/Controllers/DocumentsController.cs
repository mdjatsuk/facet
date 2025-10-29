using FacetApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _svc;
    public DocumentsController(IDocumentService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _svc.Query().ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var doc = await _svc.GetAsync(id);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        var result = await _svc.UploadAsync(file);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpGet("{id:guid}/file")]
    public IActionResult File(Guid id)
    {
        var res = _svc.GetFile(id);
        if (res is null) return NotFound();

        var fileName = res.Value.FileName ?? "file";
        var encoded = Uri.EscapeDataString(fileName);
        Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{encoded}";
        return new FileStreamResult(res.Value.Stream, res.Value.ContentType);
    }

    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id)
    {
        var res = await _svc.GetPreviewAsync(id);
        if (res is null) return NotFound();

        return Content(res, "text/html");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _svc.DeleteAsync(id);
        if (!ok) return NotFound("Fail ei leitud.");
        return NoContent(); 
    }

    public class RedactRequest { public List<string>? Values { get; set; } }

    [HttpPost("{id:guid}/redact")]
    public async Task<IActionResult> Redact(Guid id, [FromBody] RedactRequest req)
    {
        var values = req?.Values ?? new List<string>();
        var res = await _svc.RedactPdfAsync(id, values);
        if (res is null) return NotFound();

        var stream = res.Value.Stream;
        var fileName = res.Value.FileName ?? "redacted.pdf";
        var encoded = Uri.EscapeDataString(fileName);
        Response.Headers["Content-Disposition"] = $"attachment; filename*=UTF-8''{encoded}";
        return new FileStreamResult(stream, "application/pdf");
    }

    [HttpPost("{id:guid}/redact/save")]
    public async Task<IActionResult> RedactAndSave(Guid id, [FromBody] RedactRequest req)
    {
        var values = req?.Values ?? new List<string>();
        var res = await _svc.CreateRedactedCopyAsync(id, values);
        if (!res.Success) return BadRequest(res.Message);
        return Ok(res);
    }

}
